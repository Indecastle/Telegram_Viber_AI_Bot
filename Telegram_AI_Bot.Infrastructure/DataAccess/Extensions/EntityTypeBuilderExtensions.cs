using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query;

namespace MyTemplate.App.Infrastructure.DataAccess.Extensions;

internal static class EntityTypeBuilderExtensions
{
    public static EntityTypeBuilder AddQueryFilter<T>(this EntityTypeBuilder builder, Expression<Func<T, bool>> expression)
        where T : class
    {
        var lambdaExpression = CreateAggregatedQueryFilterExpression(builder, expression);
        builder.HasQueryFilter(lambdaExpression);
        return builder;
    }

    private static LambdaExpression CreateAggregatedQueryFilterExpression<T>(EntityTypeBuilder builder, Expression<Func<T, bool>> expression)
    {
        var parameterType = Expression.Parameter(builder.Metadata.ClrType);
        var expressionFilter = ReplacingExpressionVisitor.Replace(expression.Parameters.Single(), parameterType, expression.Body);

        var currentQueryFilter = builder.Metadata.GetQueryFilter();
        if (currentQueryFilter == null)
        {
            return Expression.Lambda(expressionFilter, parameterType);
        }

        var currentExpressionFilter = ReplacingExpressionVisitor.Replace(currentQueryFilter.Parameters.Single(), parameterType, currentQueryFilter.Body);
        expressionFilter = Expression.AndAlso(currentExpressionFilter, expressionFilter);
        return Expression.Lambda(expressionFilter, parameterType);
    }
}