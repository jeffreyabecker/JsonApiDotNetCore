//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.13.0
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from JadncFilters.g4 by ANTLR 4.13.0

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

namespace JsonApiDotNetCore.ExtendedQuery.QueryLanguage {
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IToken = Antlr4.Runtime.IToken;

/// <summary>
/// This interface defines a complete generic visitor for a parse tree produced
/// by <see cref="JadncFiltersParser"/>.
/// </summary>
/// <typeparam name="Result">The return type of the visit operation.</typeparam>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.13.0")]
[System.CLSCompliant(false)]
public interface IJadncFiltersVisitor<Result> : IParseTreeVisitor<Result> {
	/// <summary>
	/// Visit a parse tree produced by the <c>ofTypeExpr</c>
	/// labeled alternative in <see cref="JadncFiltersParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOfTypeExpr([NotNull] JadncFiltersParser.OfTypeExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>hasExpression</c>
	/// labeled alternative in <see cref="JadncFiltersParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitHasExpression([NotNull] JadncFiltersParser.HasExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>inExpr</c>
	/// labeled alternative in <see cref="JadncFiltersParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitInExpr([NotNull] JadncFiltersParser.InExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>nestedExpr</c>
	/// labeled alternative in <see cref="JadncFiltersParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNestedExpr([NotNull] JadncFiltersParser.NestedExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>orExpr</c>
	/// labeled alternative in <see cref="JadncFiltersParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitOrExpr([NotNull] JadncFiltersParser.OrExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>greaterLessExpr</c>
	/// labeled alternative in <see cref="JadncFiltersParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitGreaterLessExpr([NotNull] JadncFiltersParser.GreaterLessExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>functionExpr</c>
	/// labeled alternative in <see cref="JadncFiltersParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFunctionExpr([NotNull] JadncFiltersParser.FunctionExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>notExpr</c>
	/// labeled alternative in <see cref="JadncFiltersParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNotExpr([NotNull] JadncFiltersParser.NotExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>addExpr</c>
	/// labeled alternative in <see cref="JadncFiltersParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAddExpr([NotNull] JadncFiltersParser.AddExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>isNullExpr</c>
	/// labeled alternative in <see cref="JadncFiltersParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIsNullExpr([NotNull] JadncFiltersParser.IsNullExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>literalExpr</c>
	/// labeled alternative in <see cref="JadncFiltersParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLiteralExpr([NotNull] JadncFiltersParser.LiteralExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>mulExpr</c>
	/// labeled alternative in <see cref="JadncFiltersParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMulExpr([NotNull] JadncFiltersParser.MulExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>likeExpr</c>
	/// labeled alternative in <see cref="JadncFiltersParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLikeExpr([NotNull] JadncFiltersParser.LikeExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ifExpr</c>
	/// labeled alternative in <see cref="JadncFiltersParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIfExpr([NotNull] JadncFiltersParser.IfExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>equalExpr</c>
	/// labeled alternative in <see cref="JadncFiltersParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEqualExpr([NotNull] JadncFiltersParser.EqualExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>andExpr</c>
	/// labeled alternative in <see cref="JadncFiltersParser.expr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAndExpr([NotNull] JadncFiltersParser.AndExprContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="JadncFiltersParser.identifier"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIdentifier([NotNull] JadncFiltersParser.IdentifierContext context);
}
} // namespace JsonApiDotNetCore.ExtendedQuery.QueryLanguage
