using System;
using System.Linq.Expressions;
using FiltersGSOProto.V1;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace LibraryProto.Helpers.V1
{
    public static class FiltrExtenssion
    {
        static string GetExpression(this FiltrOperationType type, string columnName, string? value)
        {


            //Переменная учет ригистра - (true - учитываем, false - не учитываем)
            var letterCase = false;
            var letterCaseLike = " like ";
            if (!letterCase)
            {
                letterCaseLike = " ilike ";
            }

            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Invalid argument 'value'");
            return type switch
            {
                FiltrOperationType.Equal => $"{columnName} = '{value}'",
                FiltrOperationType.NotEqual => $"{columnName} <> '{value}'",
                FiltrOperationType.OrEqual => $"{columnName} = '{value}'",
                FiltrOperationType.OrNotEqual => $"{columnName} <> '{value}'",
                FiltrOperationType.Contains => $"{columnName} {letterCaseLike} '%{value}%'",
                FiltrOperationType.NotContains => $"{columnName} not {letterCaseLike} '%{value}%'",
                FiltrOperationType.Greater => $"{columnName} > '{value}'",
                FiltrOperationType.Less => $"{columnName} < '{value}'",
                FiltrOperationType.GreaterOrEqual => $"{columnName} >= '{value}'",
                FiltrOperationType.LeesOrEqual => $"{columnName} <= '{value}'",
                _ => throw new ArgumentException("Invalid argument 'FiltrOperationType'"),
            };
        }

        public static string CreateQueryStringFromRepeatedString(this RepeatedField<FiltrValueAndTypeField> values, string columnName, string? queryStr)
        {
            List<string> queryList = new();
            string? _value = string.Empty;
            string? response = string.Empty;

            foreach (var item in values.Where(x => x.Type != FiltrOperationType.OrEqual && x.Type != FiltrOperationType.OrNotEqual && x.Type != FiltrOperationType.Range))
            {
                if (!string.IsNullOrEmpty(item.KeyValue))
                    _value = item.KeyValue?.Replace("'", "\"");
                else
                    _value = item.Value?.Replace("'", "\"");

                queryList.Add(item.Type.GetExpression(columnName, _value));
            }

            response = queryList.Count > 0 ? $"{string.Join(" and ", queryList)}" : "";

            if (!string.IsNullOrEmpty(response))
            {
                if (!string.IsNullOrEmpty(queryStr))
                    queryStr = $"{queryStr} and {response}";
                else
                    queryStr = response;
            }


            queryList.Clear();

            foreach (var item in values.Where(x => x.Type == FiltrOperationType.OrEqual || x.Type == FiltrOperationType.OrNotEqual))
            {
                _value = item.Value?.Replace("'", "\"");
                if (!string.IsNullOrEmpty(item.KeyValue))
                    _value = item.KeyValue?.Replace("'", "\"");
                queryList.Add(item.Type.GetExpression(columnName, _value));
            }
            if (queryList.Count > 0)
            {
                string orStr = $"{string.Join(" or ", queryList)}";
                if (!string.IsNullOrEmpty(queryStr))
                    queryStr = $"{queryStr} or {orStr}";
                else
                    queryStr = orStr;
            }
            return !string.IsNullOrEmpty(queryStr) ? $" {queryStr} " : string.Empty;
        }

        public static string AggregateQueryStringFromRepeatedString(this RepeatedField<FiltrDynamicAndTypeField> values, string aggregateString)
        {
            List<string> queryList = new();
            string? _value = string.Empty;
            string? response = string.Empty;

            string queryFieldName = string.Empty;
            string queryFieldValue = string.Empty;

            string separator = "";
            foreach (var item in values.Where(x => x.Type != FiltrOperationType.OrEqual && x.Type != FiltrOperationType.OrNotEqual && x.Type != FiltrOperationType.Range))
            {

                if (item.Type == FiltrOperationType.Equal)
                {
                    //queryList.Add(
                    //"("
                    //+ aggregateString + $@" ~ '({item.Key}\\|\\|)({item.Value})(\\|\\|\\|){{1}}'"
                    //+ " or "
                    //+ aggregateString + $@" ~ '({item.Key}\\|\\|)({item.Value})$'"
                    //+ ")");

                    separator = queryList.Count > 0 ? " and " : "";

                    queryList.Add(separator 
                    + "("
                    + aggregateString + $@" ~ '(({item.Key}\|\|{item.Value})$)|(({item.Key}\|\|{item.Value})\|\|\|)'"
                    + ")");
                }
               

                if (item.Type == FiltrOperationType.NotEqual)
                {
                    separator = queryList.Count > 0 ? " and " : "";

                    queryList.Add(separator
                    + "("
                    + aggregateString + $@" ~ '({item.Key}\|\|)(?!({item.Value}))'"
                    + ")");
                }
                
                if (item.Type == FiltrOperationType.NotContains)
                {
                    separator = queryList.Count > 0 ? " and " : "";

                    queryList.Add(separator
                    + "("
                    + aggregateString + $@" ~ '({item.Key}\|\|)((?:(?!{item.Value})\w)+)[^((?:(?!{item.Value})\w)+)]'"
                    + ")");
                }

                if (item.Type == FiltrOperationType.Contains)
                {
                    separator = queryList.Count > 0 ? " and " : "";

                    //queryList.Add(separator
                    //+ "("
                    //+ aggregateString + $@" ~ '({item.Key}\\|\\|)(.*?)({item.Value})(.*?)(\\|\\|\\|){{1}}'"
                    //+ " or "
                    //+ aggregateString + $@" ~ '({item.Key}\\|\\|)(.*?)({item.Value})(.*?)$'"
                    //+ ")");

                    queryList.Add(separator
                    + "("
                    + aggregateString + $@" ~ '(({item.Key}\|\|(.*?){item.Value}(.*?))\|\|\|)|(({item.Key}\|\|(.*?){item.Value}(.*?))$)'"
                    + ")");
                }

            }

            foreach (var item in values.Where(x => x.Type == FiltrOperationType.OrEqual || x.Type == FiltrOperationType.OrNotEqual))
            {

                if (item.Type == FiltrOperationType.OrEqual)
                {
                    separator = queryList.Count > 0 ? " or " : "";

                    queryList.Add(separator
                    + "("
                    + aggregateString + $@" ~ '(({item.Key}\|\|{item.Value})$)|(({item.Key}\|\|{item.Value})\|\|\|)'"
                    + ")");
                }

                if (item.Type == FiltrOperationType.OrNotEqual)
                {
                    separator = queryList.Count > 0 ? " or " : "";

                    queryList.Add(separator
                    + "("
                    + aggregateString + $@" ~ '({item.Key}\|\|)(?!({item.Value}))'"
                    + ")");
                }

            }

            //response = queryList.Count > 0 ? $"{string.Join(" and ", queryList)}" : "";
            response = queryList.Count > 0 ? $"{string.Join(" ", queryList)}" : "";
            queryList.Clear();

            return !string.IsNullOrEmpty(response) ? $" ({response}) " : string.Empty;
        }

        public static string CreateQueryStringFromRepeatedDataTime(this RepeatedField<FiltrDataTimeRange> values, string columnName, string? queryStr)
        {
            List<string> queryList = new();
            string? response = string.Empty;

            foreach (var item in values.Where(x => x.Type != FiltrOperationType.OrEqual && x.Type != FiltrOperationType.OrNotEqual && x.Type != FiltrOperationType.Range))
            {
                queryList.Add(item.Type.GetExpression(columnName, $"{item.DateStart?.ToDateTime()}"));
            }

            foreach (var item in values.Where(x => x.Type == FiltrOperationType.Range))
            {
                queryList.Add($"({columnName}>='{item.DateStart?.ToDateTime()}' and {columnName}<='{item.DateEnd?.ToDateTime()}')");
            }

            response = queryList.Count > 0 ? $"{string.Join(" and ", queryList)}" : "";

            if (!string.IsNullOrEmpty(response))
            {
                if (!string.IsNullOrEmpty(queryStr))
                    queryStr = $"{queryStr} and {response}";
                else
                    queryStr = response;
            }

            queryList.Clear();

            foreach (var item in values.Where(x => x.Type == FiltrOperationType.OrEqual || x.Type == FiltrOperationType.OrNotEqual))
            {
                queryList.Add(item.Type.GetExpression(columnName, $"{item.DateStart?.ToDateTime()}"));
            }
            if (queryList.Count > 0)
            {
                string orStr = $"{string.Join(" or ", queryList)}";
                if (!string.IsNullOrEmpty(queryStr))
                    queryStr = $"{queryStr} or {orStr}";
                else
                    queryStr = orStr;
            }
            return !string.IsNullOrEmpty(queryStr) ? $" {queryStr} " : string.Empty;
        }
        public static string CreateQueryStringFromRepeatedDataOnly(this RepeatedField<FiltrDataOnlyRange> values, string columnName, string? queryStr)
        {
            List<string> queryList = new();
            string? response = string.Empty;

            foreach (var item in values.Where(x => x.Type != FiltrOperationType.OrEqual && x.Type != FiltrOperationType.OrNotEqual && x.Type != FiltrOperationType.Range))
            {
                queryList.Add(item.Type.GetExpression(columnName, $"{item.DateStart?.ToDateTime().Date}"));
            }

            foreach (var item in values.Where(x => x.Type == FiltrOperationType.Range))
            {
                queryList.Add($"({columnName}>='{item.DateStart?.ToDateTime().Date}' and {columnName}<='{item.DateEnd?.ToDateTime().Date}')");
            }

            response = queryList.Count > 0 ? $"{string.Join(" and ", queryList)}" : "";

            if (!string.IsNullOrEmpty(response))
            {
                if (!string.IsNullOrEmpty(queryStr))
                    queryStr = $"{queryStr} and {response}";
                else
                    queryStr = response;
            }

            queryList.Clear();

            foreach (var item in values.Where(x => x.Type == FiltrOperationType.OrEqual || x.Type == FiltrOperationType.OrNotEqual))
            {
                queryList.Add(item.Type.GetExpression(columnName, $"{item.DateStart?.ToDateTime().Date}"));
            }
            if (queryList.Count > 0)
            {
                string orStr = $"{string.Join(" or ", queryList)}";
                if (!string.IsNullOrEmpty(queryStr))
                    queryStr = $"{queryStr} or {orStr}";
                else
                    queryStr = orStr;
            }
            return !string.IsNullOrEmpty(queryStr) ? $" {queryStr} " : string.Empty;
        }

        public static string CreateQueryStringFromRepeatedTime(this RepeatedField<FiltrTimeRange> values, string columnName, string? queryStr)
        {
            List<string> queryList = new();
            string? response = string.Empty;

            foreach (var item in values.Where(x => x.Type != FiltrOperationType.OrEqual && x.Type != FiltrOperationType.OrNotEqual && x.Type != FiltrOperationType.Range))
            {
                queryList.Add(item.Type.GetExpression(columnName, $"{item.TimeStart?.ToTimeSpan()}"));
            }

            foreach (var item in values.Where(x => x.Type == FiltrOperationType.Range))
            {
                queryList.Add($"({columnName}>='{item.TimeStart?.ToTimeSpan()}' and {columnName}<='{item.TimeEnd?.ToTimeSpan()}')");
            }

            response = queryList.Count > 0 ? $"{string.Join(" and ", queryList)}" : "";

            if (!string.IsNullOrEmpty(response))
            {
                if (!string.IsNullOrEmpty(queryStr))
                    queryStr = $"{queryStr} and {response}";
                else
                    queryStr = response;
            }

            queryList.Clear();

            foreach (var item in values.Where(x => x.Type == FiltrOperationType.OrEqual || x.Type == FiltrOperationType.OrNotEqual))
            {
                queryList.Add(item.Type.GetExpression(columnName, $"{item.TimeStart?.ToTimeSpan()}"));
            }
            if (queryList.Count > 0)
            {
                string orStr = $"{string.Join(" or ", queryList)}";
                if (!string.IsNullOrEmpty(queryStr))
                    queryStr = $"{queryStr} or {orStr}";
                else
                    queryStr = orStr;
            }
            return !string.IsNullOrEmpty(queryStr) ? $" {queryStr} " : string.Empty;
        }

        public static string CreateQueryStringFromRepeatedInt(this RepeatedField<FiltrIntAndTypeField> values, string columnName, string? queryStr)
        {
            List<string> queryList = new();
            string? response = string.Empty;

            foreach (var item in values.Where(x => x.Type != FiltrOperationType.OrEqual && x.Type != FiltrOperationType.OrNotEqual && x.Type != FiltrOperationType.Range))
            {
                queryList.Add(item.Type.GetExpression(columnName, $"{item.Value}"));
            }

            foreach (var item in values.Where(x => x.Type == FiltrOperationType.Range))
            {
                queryList.Add($"({columnName}>='{item.Value}' and {columnName}<='{item.MaxValue}')");
            }

            response = queryList.Count > 0 ? $"{string.Join(" and ", queryList)}" : "";

            if (!string.IsNullOrEmpty(response))
            {
                if (!string.IsNullOrEmpty(queryStr))
                    queryStr = $"{queryStr} and {response}";
                else
                    queryStr = response;
            }

            queryList.Clear();

            foreach (var item in values.Where(x => x.Type == FiltrOperationType.OrEqual || x.Type == FiltrOperationType.OrNotEqual))
            {
                queryList.Add(item.Type.GetExpression(columnName, $"{item.Value}"));
            }
            if (queryList.Count > 0)
            {
                string orStr = $"{string.Join(" or ", queryList)}";
                if (!string.IsNullOrEmpty(queryStr))
                    queryStr = $"{queryStr} or {orStr}";
                else
                    queryStr = orStr;
            }
            return !string.IsNullOrEmpty(queryStr) ? $" {queryStr} " : string.Empty;
        }

        #region For Entity
        public static BinaryExpression? CreateExpressionFromRepeatedString(this RepeatedField<FiltrValueAndTypeField> values, Expression field, BinaryExpression? filter)
        {
            return values.CreateExpression(field, filter);
        }

        public static BinaryExpression? CreateExpressionFromRepeatedInt(this RepeatedField<FiltrIntAndTypeField> values, Expression field, BinaryExpression? filter)
        {
            return values.CreateExpression(field, filter);
        }

        public static BinaryExpression? CreateExpressionFromRepeatedDataTime(this RepeatedField<FiltrDataTimeRange> values, Expression field, BinaryExpression? filter)
        {
            return values.CreateExpression(field, filter);
        }

        public static BinaryExpression? CreateExpressionFromRepeatedTime(this RepeatedField<FiltrTimeRange> values, Expression field, BinaryExpression? filter)
        {
            return values.CreateExpression(field, filter);
        }

        public static BinaryExpression? CreateExpressionFromRepeatedDuration(this RepeatedField<FiltrDurationRange> values, Expression field, BinaryExpression? filter)
        {
            return values.CreateExpression(field, filter);
        }

        static BinaryExpression? CreateExpression<TData>(this RepeatedField<TData> values, Expression field, BinaryExpression? filter) where TData : IMessage
        {
            FiltrOperationType filtrType = FiltrOperationType.None;
            System.Type fieldType = typeof(string);

            foreach (var item in values)
            {
                BinaryExpression? binaryExpressionAnd = null;
                BinaryExpression? binaryExpressionOr = null;

                ConstantExpression? constantExpression = null;
                ConstantExpression? constantExpressionMax = null;

                if (item is FiltrValueAndTypeField)
                {
                    var member = (item as FiltrValueAndTypeField);
                    if (member == null)
                        continue;
                    filtrType = member.Type;
                    fieldType = member.Value.GetType();
                    var _value = !string.IsNullOrEmpty(member.KeyValue) ? member.KeyValue : member.Value;
                    constantExpression = Expression.Constant(_value, fieldType);
                }
                else if (item is FiltrDataTimeRange)
                {
                    var member = (item as FiltrDataTimeRange);
                    if (member == null)
                        continue;
                    filtrType = member.Type;
                    fieldType = member.DateStart.GetType();
                    constantExpression = Expression.Constant(member.DateStart, fieldType);
                    if (member.DateEnd != null)
                        constantExpressionMax = Expression.Constant(member.DateEnd, fieldType);
                }
                else if (item is FiltrDataOnlyRange)
                {
                    var member = (item as FiltrDataOnlyRange);
                    if (member == null)
                        continue;
                    filtrType = member.Type;
                    fieldType = member.DateStart.GetType();
                    constantExpression = Expression.Constant(member.DateStart, fieldType);
                    if (member.DateEnd != null)
                        constantExpressionMax = Expression.Constant(member.DateEnd, fieldType);
                }
                else if (item is FiltrTimeRange)
                {
                    var member = (item as FiltrTimeRange);
                    if (member == null)
                        continue;
                    filtrType = member.Type;
                    fieldType = typeof(TimeSpan?);
                    constantExpression = Expression.Constant(member.TimeStart.ToTimeSpan(), fieldType);
                    if (member.TimeEnd != null)
                        constantExpressionMax = Expression.Constant(member.TimeEnd.ToTimeSpan(), fieldType);
                }
                else if (item is FiltrDurationRange)
                {
                    var member = (item as FiltrDurationRange);
                    if (member == null)
                        continue;
                    filtrType = member.Type;
                    fieldType = typeof(TimeSpan?);
                    constantExpression = Expression.Constant(member.Start.ToTimeSpan(), fieldType);
                    if (member.End != null)
                        constantExpressionMax = Expression.Constant(member.End.ToTimeSpan(), fieldType);
                }
                else if (item is FiltrIntAndTypeField)
                {
                    var member = (item as FiltrIntAndTypeField);
                    if (member == null)
                        continue;
                    filtrType = member.Type;
                    fieldType = field.Type; //member.Value.GetType();                    
                    constantExpression = Expression.Constant(Convert.ChangeType(member.Value, fieldType), fieldType);
                    if (member.MaxValue > 0)
                        constantExpressionMax = Expression.Constant(Convert.ChangeType(member.MaxValue, fieldType), fieldType);
                }

                if (constantExpression == null)
                    continue;


                if (filtrType == FiltrOperationType.Contains)
                {
                    binaryExpressionAnd = Expression.MakeBinary(ExpressionType.Equal, field, constantExpression, false, typeof(FiltrExtenssion).GetMethod("Containss", new[] { fieldType, fieldType }));
                }
                else if (filtrType == FiltrOperationType.NotContains)
                {
                    binaryExpressionAnd = Expression.MakeBinary(ExpressionType.Equal, field, constantExpression, false, typeof(FiltrExtenssion).GetMethod("NoContainss", new[] { fieldType, fieldType }));
                }
                else if (filtrType == FiltrOperationType.OrEqual)
                {
                    binaryExpressionOr = Expression.Equal(field, constantExpression);
                }
                else if (filtrType == FiltrOperationType.OrNotEqual)
                {
                    binaryExpressionOr = Expression.NotEqual(field, constantExpression);
                }
                else if (filtrType == FiltrOperationType.Range)
                {
                    filter = filter.AddBinaryExpression(Expression.GreaterThanOrEqual(field, constantExpression));
                    if (constantExpressionMax != null)
                    {
                        binaryExpressionAnd = Expression.LessThanOrEqual(field, constantExpressionMax);
                    }
                }
                else
                {
                    binaryExpressionAnd = Expression.MakeBinary((ExpressionType)filtrType, field, constantExpression, false, null);
                }

                if (binaryExpressionAnd != null)
                    filter = filter.AddBinaryExpression(binaryExpressionAnd);
                if (binaryExpressionOr != null)
                    filter = filter.OrBinaryExpression(binaryExpressionOr);
            }
            return filter;
        }

        public static BinaryExpression? AddBinaryExpression(this BinaryExpression? filter, BinaryExpression? newExpression)
        {
            if (newExpression != null)
                filter = (filter != null) ? Expression.And(filter, newExpression) : newExpression;
            return filter;
        }
        public static BinaryExpression? OrBinaryExpression(this BinaryExpression? filter, BinaryExpression? newExpression)
        {
            if (newExpression != null)
                filter = (filter != null) ? Expression.Or(filter, newExpression) : newExpression;

            return filter;
        }

        public static bool Containss(string text, string text2)
        {
            return text.Contains(text2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool NoContainss(string text, string text2)
        {
            return !text.Contains(text2, StringComparison.OrdinalIgnoreCase);
        }
        #endregion

    }
}
