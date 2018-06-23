﻿using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Task = System.Threading.Tasks.Task;

namespace Codist.Commands
{
	/// <summary>A command which takes screenshot of the active code document window.</summary>
	internal sealed class ScreenshotCommand
	{
		public static readonly Guid guidIWpfTextViewHost = new Guid("8C40265E-9FDB-4f54-A0FD-EBB72B7D0476");
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x0100;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("d668a130-cb52-4143-b389-55560823f3d6");

		/// <summary>
		/// VS Package that provides this command, not null.
		/// </summary>
		readonly Package package;

		/// <summary>
		/// Initializes a new instance of the <see cref="ScreenshotCommand"/> class.
		/// Adds our command handlers for menu (commands must exist in the command table file)
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		/// <param name="commandService">Command service to add command to, not null.</param>
		private ScreenshotCommand(Package package, OleMenuCommandService commandService) {
			this.package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new OleMenuCommand(Execute, menuCommandID);
			menuItem.BeforeQueryStatus += (s, args) => {
				var c = s as OleMenuCommand;
				c.Enabled = CodistPackage.DTE.ActiveDocument != null;
			};
			commandService.AddCommand(menuItem);
		}

		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static ScreenshotCommand Instance {
			get;
			private set;
		}

		/// <summary>
		/// Initializes the singleton instance of the command.
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		public static void Initialize(Package package) {
			// Verify the current thread is the UI thread - the call to AddCommand in ScreenshotCommand's constructor requires
			// the UI thread.
			ThreadHelper.ThrowIfNotOnUIThread();

			OleMenuCommandService commandService = (package as IServiceProvider).GetService((typeof(IMenuCommandService))) as OleMenuCommandService;
			Instance = new ScreenshotCommand(package, commandService);
		}

		/// <summary>
		/// This function is the callback used to execute the command when the menu item is clicked.
		/// See the constructor to see how the menu item is associated with this function using
		/// OleMenuCommandService service and MenuCommand class.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="e">Event args.</param>
		private void Execute(object sender, EventArgs e) {
			ThreadHelper.ThrowIfNotOnUIThread();
			var doc = CodistPackage.DTE.ActiveDocument;
			if (doc == null) {
				return;
			}
			var textView = GetIVsTextView(doc.FullName);
			if (textView == null) {
				return;
			}
			var docWindow = GetWpfTextView(textView);

			using (var f = new System.Windows.Forms.SaveFileDialog { Filter = "PNG images (*.png)|*.png", AddExtension = true, Title = "Please specify the location of the screenshot file", FileName = System.IO.Path.GetFileNameWithoutExtension(doc.Name) + ".png" }) {
				if (f.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
					try {
						WpfHelper.ScreenShot(docWindow.VisualElement.GetVisualParent<System.Windows.Controls.Grid>(), f.FileName);
					}
					catch (Exception ex) {
						VsShellUtilities.ShowMessageBox(
							package,
							"Failed to save screenshot for " + doc.Name + "\n" + ex.Message,
							nameof(Codist),
							OLEMSGICON.OLEMSGICON_INFO,
							OLEMSGBUTTON.OLEMSGBUTTON_OK,
							OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

					}
				}
			}
		}
		internal IVsTextView GetIVsTextView(string filePath) {
			var dte2 = (EnvDTE80.DTE2)Package.GetGlobalService(typeof(SDTE));
			var sp = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte2;
			var serviceProvider = new ServiceProvider(sp);

			IVsUIHierarchy uiHierarchy;
			uint itemID;
			IVsWindowFrame windowFrame;
			if (VsShellUtilities.IsDocumentOpen(package, filePath, Guid.Empty,
											out uiHierarchy, out itemID, out windowFrame)) {
				// Get the IVsTextView from the windowFrame.
				return VsShellUtilities.GetTextView(windowFrame);
			}

			return null;
		}
		static IWpfTextView GetWpfTextView(IVsTextView vTextView) {
			IWpfTextView view = null;
			IVsUserData userData = vTextView as IVsUserData;

			if (null != userData) {
				IWpfTextViewHost viewHost;
				object holder;
				Guid guidViewHost = guidIWpfTextViewHost;
				userData.GetData(ref guidViewHost, out holder);
				viewHost = (IWpfTextViewHost)holder;
				view = viewHost.TextView;
			}

			return view;
		}
	}
}
