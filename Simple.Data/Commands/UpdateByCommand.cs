﻿using System;
using System.Dynamic;
using System.Linq;
using Simple.Data.Extensions;

namespace Simple.Data.Commands
{
    using System.Collections.Generic;
    using Operations;

    class UpdateByCommand : ICommand
    {
        public bool IsCommandFor(string method)
        {
            return method.Homogenize().StartsWith("updateby", StringComparison.InvariantCultureIgnoreCase);
        }

        public object Execute(DataStrategy dataStrategy, DynamicTable table, InvokeMemberBinder binder, object[] args)
        {
            if (binder.HasSingleUnnamedArgument())
            {
                return UpdateByKeyFields(table.GetQualifiedName(), dataStrategy, args[0],
                                                MethodNameParser.ParseCriteriaNamesFromMethodName(binder.Name));
            }

            var criteria = MethodNameParser.ParseFromBinder(binder, args);
            var criteriaExpression = ExpressionHelper.CriteriaDictionaryToExpression(table.GetQualifiedName(), criteria);
            var data = binder.NamedArgumentsToDictionary(args)
                .Where(kvp => !criteria.ContainsKey(kvp.Key))
                .ToDictionary();
            return dataStrategy.Run.Execute(new UpdateEntityOperation(table.GetQualifiedName(), data.ToReadOnly()));
        }

        internal static object UpdateByKeyFields(string tableName, DataStrategy dataStrategy, object entity, IEnumerable<string> keyFieldNames)
        {
            var record = UpdateCommand.ObjectToDictionary(entity);
            var list = record as ICollection<IDictionary<string, object>>;
            if (list != null) return dataStrategy.Run.Execute(new UpdateEntityOperation(tableName, list.Select(d => d.ToReadOnly()).ToList()));

            var dict = record as IDictionary<string, object>;
            return dataStrategy.Run.Execute(new UpdateEntityOperation(tableName, dict.ToReadOnly()));
        }

        private static IEnumerable<KeyValuePair<string, object>> GetCriteria(IEnumerable<string> keyFieldNames, IDictionary<string, object> record)
        {
            var criteria = new Dictionary<string, object>();

            foreach (var keyFieldName in keyFieldNames)
            {
                var name = keyFieldName;
                var keyValuePair = record.SingleOrDefault(kvp => kvp.Key.Homogenize().Equals(name.Homogenize()));
                if (string.IsNullOrWhiteSpace(keyValuePair.Key))
                {
                    throw new InvalidOperationException("Key field value not set.");
                }

                criteria.Add(keyFieldName, keyValuePair.Value);
                record.Remove(keyValuePair);
            }
            return criteria;
        }
    }
}
