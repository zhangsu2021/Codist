﻿using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;

namespace Codist
{
	static partial class CodeAnalysisHelper
	{
		public static Document GetDocument(this Workspace workspace, SnapshotSpan span) {
			if (workspace == null) {
				throw new ArgumentNullException("workspace");
			}
			var solution = workspace.CurrentSolution;
			if (solution == null) {
				throw new InvalidOperationException("solution is null");
			}
			if (span.Snapshot == null) {
				throw new InvalidOperationException("snapshot is null");
			}
			var sourceText = span.Snapshot.AsText();
			if (sourceText == null) {
				throw new InvalidOperationException("sourceText is null");
			}
			var docId = workspace.GetDocumentIdInCurrentContext(sourceText.Container);
			if (docId == null) {
				throw new InvalidOperationException("docId is null");
			}
			return solution.ContainsDocument(docId)
				? solution.GetDocument(docId)
				: solution.WithDocumentText(docId, sourceText, PreservationMode.PreserveIdentity).GetDocument(docId);
		}

		public static ISymbol GetSymbolExt(this SemanticModel semanticModel, SyntaxNode node) {
			return node.IsDeclaration() || node.Kind() == SyntaxKind.VariableDeclarator ? semanticModel.GetDeclaredSymbol(node) :
					(node is AttributeArgumentSyntax
						? semanticModel.GetSymbolInfo((node as AttributeArgumentSyntax).Expression).Symbol
						: null)
					?? (node is SimpleBaseTypeSyntax || node is TypeConstraintSyntax
						? semanticModel.GetSymbolInfo(node.FindNode(node.Span, false, true)).Symbol
						: null)
					?? (node is ArgumentListSyntax
						? semanticModel.GetSymbolInfo(node.Parent).Symbol
						: null)
					?? (node.Parent is MemberAccessExpressionSyntax
						? semanticModel.GetSymbolInfo(node.Parent).CandidateSymbols.FirstOrDefault()
						: null)
					?? (node.Parent is ArgumentSyntax
						? semanticModel.GetSymbolInfo((node.Parent as ArgumentSyntax).Expression).CandidateSymbols.FirstOrDefault()
						: null)
					?? (node is AccessorDeclarationSyntax
						? semanticModel.GetDeclaredSymbol(node.Parent.Parent)
						: null)
					?? (node is TypeParameterSyntax || node is ParameterSyntax ? semanticModel.GetDeclaredSymbol(node) : null);
		}

		public static ISymbol GetSymbolOrFirstCandidate(this SemanticModel semanticModel, SyntaxNode node) {
			var info = semanticModel.GetSymbolInfo(node);
			return info.Symbol
				?? (info.CandidateSymbols.Length > 0 ? info.CandidateSymbols[0] : null);
		}

		public static void OpenFile(this EnvDTE.DTE dte, string file, int line, int column) {
			if (String.IsNullOrEmpty(file)) {
				return;
			}
			file = System.IO.Path.GetFullPath(file);
			if (System.IO.File.Exists(file) == false) {
				return;
			}
			using (new NewDocumentStateScope(__VSNEWDOCUMENTSTATE.NDS_Provisional, VSConstants.NewDocumentStateReason.Navigation)) {
				dte.ItemOperations.OpenFile(file);
				((EnvDTE.TextSelection)dte.ActiveDocument.Selection).MoveToLineAndOffset(line, column);
			}
		}

		public static bool IsDeclaration(this SyntaxNode node) {
			switch (node.Kind()) {
				case SyntaxKind.ClassDeclaration:
				case SyntaxKind.ConstructorDeclaration:
				case SyntaxKind.ConversionOperatorDeclaration:
				case SyntaxKind.DelegateDeclaration:
				case SyntaxKind.DestructorDeclaration:
				case SyntaxKind.EnumDeclaration:
				case SyntaxKind.EnumMemberDeclaration:
				case SyntaxKind.EventDeclaration:
				case SyntaxKind.EventFieldDeclaration:
				case SyntaxKind.FieldDeclaration:
				case SyntaxKind.IndexerDeclaration:
				case SyntaxKind.InterfaceDeclaration:
				case SyntaxKind.LocalDeclarationStatement:
				case SyntaxKind.MethodDeclaration:
				case SyntaxKind.NamespaceDeclaration:
				case SyntaxKind.OperatorDeclaration:
				case SyntaxKind.PropertyDeclaration:
				case SyntaxKind.StructDeclaration:
				case SyntaxKind.VariableDeclaration:
				//case SyntaxKind.VariableDeclarator:
					return true;
			}
			return false;
		}

		public static bool IsSyntaxBlock(this SyntaxNode node) {
			switch (node.Kind()) {
				case SyntaxKind.ArgumentList:
				case SyntaxKind.AttributeArgumentList:
				//case SyntaxKind.Block:
				case SyntaxKind.DoStatement:
				case SyntaxKind.FixedStatement:
				case SyntaxKind.ForEachStatement:
				case SyntaxKind.ForStatement:
				case SyntaxKind.IfStatement:
				case SyntaxKind.LockStatement:
				case SyntaxKind.SwitchStatement:
				case SyntaxKind.SwitchSection:
				case SyntaxKind.TryStatement:
				case SyntaxKind.UsingStatement:
				case SyntaxKind.WhileStatement:
				case SyntaxKind.ParameterList:
				case SyntaxKind.ParenthesizedExpression:
				case SyntaxKind.ParenthesizedLambdaExpression:
				case SyntaxKind.SimpleLambdaExpression:
				case SyntaxKind.UnsafeStatement:
				case SyntaxKind.XmlElement:
				case SyntaxKind.XmlEmptyElement:
				case SyntaxKind.XmlComment:
					return true;
			}
			return false;
		}

		public static int GetImageId(this SyntaxNode node) {
			switch (node.Kind()) {
				case SyntaxKind.ClassDeclaration: return KnownImageIds.Class;
				case SyntaxKind.EnumDeclaration: return KnownImageIds.Enumeration;
				case SyntaxKind.StructDeclaration: return KnownImageIds.Structure;
				case SyntaxKind.InterfaceDeclaration: return KnownImageIds.Interface;
				case SyntaxKind.ConstructorDeclaration: return KnownImageIds.NewItem;
				case SyntaxKind.ConversionOperatorDeclaration: return KnownImageIds.ConvertPartition;
				case SyntaxKind.DestructorDeclaration: return KnownImageIds.DeleteListItem;
				case SyntaxKind.IndexerDeclaration: return KnownImageIds.ClusteredIndex;
				case SyntaxKind.MethodDeclaration: return KnownImageIds.Method;
				case SyntaxKind.OperatorDeclaration: return KnownImageIds.Operator;
				case SyntaxKind.PropertyDeclaration: return KnownImageIds.Property;
				case SyntaxKind.FieldDeclaration: return KnownImageIds.Field;
				case SyntaxKind.VariableDeclaration: return KnownImageIds.LocalVariable;
				case SyntaxKind.NamespaceDeclaration: return KnownImageIds.Namespace;
				case SyntaxKind.ArgumentList:
				case SyntaxKind.AttributeArgumentList: return KnownImageIds.Parameter;
				case SyntaxKind.DoStatement: return KnownImageIds.DoWhile;
				case SyntaxKind.FixedStatement: return KnownImageIds.Pin;
				case SyntaxKind.ForEachStatement:
				case SyntaxKind.ForStatement: return KnownImageIds.ForEachLoop;
				case SyntaxKind.IfStatement: return KnownImageIds.If;
				case SyntaxKind.LockStatement: return KnownImageIds.Lock;
				case SyntaxKind.SwitchStatement: return KnownImageIds.FlowSwitch;
				case SyntaxKind.SwitchSection: return KnownImageIds.FlowDecision;
				case SyntaxKind.TryStatement: return KnownImageIds.TryCatch;
				case SyntaxKind.UsingStatement: return KnownImageIds.Entry;
				case SyntaxKind.WhileStatement: return KnownImageIds.While;
				case SyntaxKind.ParameterList: return KnownImageIds.Parameter;
				case SyntaxKind.ParenthesizedExpression:
				case SyntaxKind.ParenthesizedLambdaExpression:
				case SyntaxKind.SimpleLambdaExpression: return KnownImageIds.NamedSet;
				case SyntaxKind.UnsafeStatement: return KnownImageIds.HotSpot;
				case SyntaxKind.XmlElement:
				case SyntaxKind.XmlEmptyElement: return KnownImageIds.XMLElement;
				case SyntaxKind.XmlComment: return KnownImageIds.XMLCommentTag;
			}
			return KnownImageIds.UnknownMember;
		}

		public static string GetSyntaxBrief(this SyntaxNode node) {
			switch (node.Kind()) {
				case SyntaxKind.ClassDeclaration: return "class";
				case SyntaxKind.EnumDeclaration: return "enum";
				case SyntaxKind.StructDeclaration: return "struct";
				case SyntaxKind.InterfaceDeclaration: return "interface";
				case SyntaxKind.ConstructorDeclaration: return "constructor";
				case SyntaxKind.ConversionOperatorDeclaration: return "conversion operator";
				case SyntaxKind.DestructorDeclaration: return "destructor";
				case SyntaxKind.IndexerDeclaration: return "property";
				case SyntaxKind.MethodDeclaration: return "method";
				case SyntaxKind.OperatorDeclaration: return "operator";
				case SyntaxKind.PropertyDeclaration: return "property";
				case SyntaxKind.FieldDeclaration: return "field";
				case SyntaxKind.NamespaceDeclaration: return "namespace";
				case SyntaxKind.ArgumentList:
				case SyntaxKind.AttributeArgumentList: return "argument list";
				case SyntaxKind.DoStatement: return "do loop";
				case SyntaxKind.FixedStatement: return "fixed";
				case SyntaxKind.ForEachStatement: return "foreach loop";
				case SyntaxKind.ForStatement: return "for loop";
				case SyntaxKind.IfStatement: return "if statement";
				case SyntaxKind.LocalDeclarationStatement: return "local";
				case SyntaxKind.LockStatement: return "lock statement";
				case SyntaxKind.SwitchStatement: return "switch";
				case SyntaxKind.SwitchSection: return "switch section";
				case SyntaxKind.TryStatement: return "try catch";
				case SyntaxKind.UsingStatement: return "using statement";
				case SyntaxKind.WhileStatement: return "while loop";
				case SyntaxKind.ParameterList: return "parameter list";
				case SyntaxKind.ParenthesizedExpression:
				case SyntaxKind.ParenthesizedLambdaExpression:
				case SyntaxKind.SimpleLambdaExpression: return "expression";
				case SyntaxKind.UnsafeStatement: return "unsafe";
				case SyntaxKind.VariableDeclarator: return "variable";
				case SyntaxKind.XmlElement:
				case SyntaxKind.XmlEmptyElement: return "xml element";
				case SyntaxKind.XmlComment: return "xml comment";
			}
			return null;
		}

		/// <summary>Returns the object creation syntax node from an named type identifier node.</summary>
		/// <param name="node"></param>
		/// <returns>Returns the constructor node if <paramref name="node"/>'s parent is <see cref="SyntaxKind.ObjectCreationExpression"/>, otherwise, <see langword="null"/> is returned.</returns>
		public static SyntaxNode GetObjectCreationNode(this SyntaxNode node) {
			node = node.Parent;
			if (node == null) {
				return null;
			}
			var kind = node.Kind();
			if (kind == SyntaxKind.ObjectCreationExpression) {
				return node;
			}
			if (kind == SyntaxKind.QualifiedName) {
				node = node.Parent;
				if (node.IsKind(SyntaxKind.ObjectCreationExpression)) {
					return node;
				}
			}
			return null;
		}

		public static string GetDeclarationSignature(this SyntaxNode node) {
			switch (node.Kind()) {
				case SyntaxKind.ClassDeclaration:
				case SyntaxKind.EnumDeclaration:
				case SyntaxKind.StructDeclaration:
				case SyntaxKind.InterfaceDeclaration:
					return (node as BaseTypeDeclarationSyntax).Identifier.Text;

				case SyntaxKind.ConstructorDeclaration: return (node as ConstructorDeclarationSyntax).Identifier.Text;
				case SyntaxKind.ConversionOperatorDeclaration: return (node as ConversionOperatorDeclarationSyntax).OperatorKeyword.Text;
				case SyntaxKind.DestructorDeclaration: return (node as DestructorDeclarationSyntax).Identifier.Text;
				case SyntaxKind.IndexerDeclaration: return "Indexer";
				case SyntaxKind.MethodDeclaration: return (node as MethodDeclarationSyntax).Identifier.Text;
				case SyntaxKind.OperatorDeclaration: return (node as OperatorDeclarationSyntax).OperatorKeyword.Text;
				case SyntaxKind.PropertyDeclaration: return (node as PropertyDeclarationSyntax).Identifier.Text;
				case SyntaxKind.SimpleLambdaExpression: return "(" + (node as SimpleLambdaExpressionSyntax).Parameter.ToString() + ")";
				case SyntaxKind.ParenthesizedLambdaExpression: return (node as ParenthesizedLambdaExpressionSyntax).ParameterList.ToString();
				case SyntaxKind.NamespaceDeclaration: return ((node as NamespaceDeclarationSyntax).Name as IdentifierNameSyntax)?.Identifier.Text;
				case SyntaxKind.VariableDeclarator: return (node as VariableDeclaratorSyntax).Identifier.Text;
				case SyntaxKind.LocalDeclarationStatement:
				case SyntaxKind.VariableDeclaration: return "Variables";
			}
			return null;
		}

		public static bool IsLineComment(this SyntaxTrivia trivia) {
			switch (trivia.Kind()) {
				case SyntaxKind.MultiLineCommentTrivia:
				case SyntaxKind.SingleLineCommentTrivia:
					return true;
			}
			return false;
		}

		public static bool IsType(this CodeMemberType type) {
			return type > CodeMemberType.Root && type < CodeMemberType.Member;
		}

		public static bool IsMember(this CodeMemberType type) {
			return type > CodeMemberType.Member && type < CodeMemberType.Other;
		}
	}
}
