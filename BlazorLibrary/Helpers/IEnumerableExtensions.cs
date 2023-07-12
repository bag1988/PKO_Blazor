using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorLibrary.Models;
using FiltersGSOProto.V1;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Microsoft.IdentityModel.Tokens;
using SMDataServiceProto.V1;
using static Google.Protobuf.Reflection.MessageDescriptor;

namespace BlazorLibrary.Helpers
{
    public static class IEnumerableExtensions
    {
        public static TData? GetNextSelectItem<TData>(this IEnumerable<TData>? items, TData? current, int? index = 1)
        {
            TData? newSelect = default;
            if (items != null && items.Any())
            {
                if (current == null)
                {
                    newSelect = items.First();
                }
                else
                {
                    if (index == 1)
                    {
                        var newElem = items.SkipWhile(x => !x?.Equals(current) ?? false).FirstOrDefault(x => !x?.Equals(current) ?? false);
                        if (newElem == null)
                        {
                            newSelect = items.First();
                        }
                        else
                            newSelect = newElem;
                    }
                    else
                    {
                        var newElem = items.TakeWhile(x => !x?.Equals(current) ?? false).LastOrDefault(x => !x?.Equals(current) ?? false);
                        if (newElem == null)
                        {
                            newSelect = items.Last();
                        }
                        else
                            newSelect = newElem;
                    }
                }
            }
            return newSelect;
        }

        public static TData? GetNextSelectItem<TData>(this IEnumerable<TData>? items, Func<TData, bool> match)
        {
            TData? newSelect = default;
            if (items != null && items.Any())
            {
                var newElem = items.SkipWhile(match).FirstOrDefault(match);
                if (newElem == null)
                {
                    newSelect = items.Last();
                }
                else
                    newSelect = newElem;
            }
            return newSelect;
        }

        public static void AddFiltrItemToFiltr(this IMessage? protoModel, FiltrItem item)
        {
            if (protoModel == null)
                return;
            FieldDescriptor? field = protoModel.Descriptor.FindFieldByName(item.Key);
            if (field != null)
            {
                if (field.IsRepeated)
                {
                    if (field.MessageType == FiltrValueAndTypeField.Descriptor)
                    {
                        var f = (RepeatedField<FiltrValueAndTypeField>)field.Accessor.GetValue(protoModel);

                        if (!f.Any(x => x.Value.Equals(item.Value?.Value) && x.Type == item.Operation))
                        {
                            f.Add(new FiltrValueAndTypeField() { Value = item.Value?.Value, KeyValue = item.Value?.KeyValue ?? string.Empty, Type = item.Operation });
                        }
                    }
                    else if (field.MessageType == FiltrIntAndTypeField.Descriptor)
                    {
                        var f = (RepeatedField<FiltrIntAndTypeField>)field.Accessor.GetValue(protoModel);

                        var intRange = item.Value?.Value.Split("-") ?? new string[0];
                        int maxValue = 0;
                        int.TryParse(intRange[0], out var minValue);
                        if (item.Operation == FiltrOperationType.Range && intRange.Length > 0)
                        {
                            int.TryParse(intRange[1], out maxValue);
                        }
                        if (!f.Any(x => x.Value.Equals(item.Value?.Value) && x.Type == item.Operation))
                        {
                            f.Add(new FiltrIntAndTypeField() { Value = minValue, MaxValue = maxValue, Type = item.Operation });
                        }
                    }
                    else if (field.MessageType == FiltrDataTimeRange.Descriptor)
                    {
                        var f = (RepeatedField<FiltrDataTimeRange>)field.Accessor.GetValue(protoModel);

                        var dateRange = item.Value?.Value.Split("-") ?? new string[0];
                        DateTime? dateEnd = null;
                        DateTime.TryParse(dateRange[0], out var dateStart);
                        if (item.Operation == FiltrOperationType.Range && dateRange.Length > 0)
                        {
                            DateTime.TryParse(dateRange[1], out var _dateEnd);
                            dateEnd = _dateEnd;
                        }
                        if (!f.Any(x => (x.DateStart?.Equals(dateStart.ToUniversalTime().ToTimestamp()) ?? true) && (x.DateEnd?.Equals(dateEnd?.ToUniversalTime().ToTimestamp()) ?? true) && x.Type == item.Operation))
                        {
                            f.Add(new FiltrDataTimeRange() { DateStart = dateStart.ToUniversalTime().ToTimestamp(), DateEnd = dateEnd?.ToUniversalTime().ToTimestamp(), Type = item.Operation });
                        }
                    }
                    else if (field.MessageType == FiltrDataOnlyRange.Descriptor)
                    {
                        var f = (RepeatedField<FiltrDataOnlyRange>)field.Accessor.GetValue(protoModel);

                        var dateRange = item.Value?.Value.Split("-") ?? new string[0];
                        DateTime? dateEnd = null;
                        DateTime.TryParse(dateRange[0], out var dateStart);
                        if (item.Operation == FiltrOperationType.Range && dateRange.Length > 0)
                        {
                            DateTime.TryParse(dateRange[1], out var _dateEnd);
                            dateEnd = _dateEnd;
                        }
                        if (!f.Any(x => (x.DateStart?.Equals(dateStart.ToUniversalTime().ToTimestamp()) ?? true) && (x.DateEnd?.Equals(dateEnd?.ToUniversalTime().ToTimestamp()) ?? true) && x.Type == item.Operation))
                        {
                            f.Add(new FiltrDataOnlyRange() { DateStart = dateStart.ToUniversalTime().ToTimestamp(), DateEnd = dateEnd?.ToUniversalTime().ToTimestamp(), Type = item.Operation });
                        }
                    }
                    else if (field.MessageType == FiltrTimeRange.Descriptor)
                    {
                        var f = (RepeatedField<FiltrTimeRange>)field.Accessor.GetValue(protoModel);

                        var timeRange = item.Value?.Value.Split("-") ?? new string[0];
                        TimeSpan? timeEnd = null;
                        TimeSpan.TryParse(timeRange[0], out var timeStart);

                        if (item.Operation == FiltrOperationType.Range && timeRange.Length > 0)
                        {
                            TimeSpan.TryParse(timeRange[1], out var _dateEnd);
                            timeEnd = _dateEnd;
                        }
                        if (!f.Any(x => (x.TimeStart?.Equals(timeStart.LocalTimeSpanToUTC()) ?? true) && (x.TimeEnd?.Equals(timeEnd?.LocalTimeSpanToUTC()) ?? true) && x.Type == item.Operation))
                        {
                            f.Add(new FiltrTimeRange() { TimeStart = timeStart.LocalTimeSpanToUTC().ToDuration(), TimeEnd = timeEnd?.LocalTimeSpanToUTC().ToDuration(), Type = item.Operation });
                        }
                    }
                    else if (field.MessageType == FiltrDurationRange.Descriptor)
                    {
                        var f = (RepeatedField<FiltrDurationRange>)field.Accessor.GetValue(protoModel);

                        var timeRange = item.Value?.Value.Split("-") ?? new string[0];
                        TimeSpan? timeEnd = null;
                        TimeSpan.TryParse(timeRange[0], out var timeStart);

                        if (item.Operation == FiltrOperationType.Range && timeRange.Length > 0)
                        {
                            TimeSpan.TryParse(timeRange[1], out var _dateEnd);
                            timeEnd = _dateEnd;
                        }
                        if (!f.Any(x => (x.Start?.Equals(timeStart) ?? true) && (x.End?.Equals(timeEnd) ?? true) && x.Type == item.Operation))
                        {
                            f.Add(new FiltrDurationRange() { Start = timeStart.ToDuration(), End = timeEnd?.ToDuration(), Type = item.Operation });
                        }
                    }
                }
                else
                {
                    if (field.MessageType == FiltrBoolField.Descriptor)
                    {
                        field.Accessor.SetValue(protoModel, new FiltrBoolField() { ViewName = item.Value?.Value ?? "", Value = true.ToString() == item.Value?.KeyValue ? true : false });
                    }
                }
            }
            else
            {
                field = protoModel.Descriptor.Fields.InDeclarationOrder().FirstOrDefault(x => x.MessageType == FiltrDynamicAndTypeField.Descriptor);
                if (field?.IsRepeated ?? false)
                {
                    var f = (RepeatedField<FiltrDynamicAndTypeField>)field.Accessor.GetValue(protoModel);

                    if (!f.Any(x => x.Key == item.Key && x.Value.Equals(item.Value?.Value) && x.Type == item.Operation))
                    {
                        f.Add(new FiltrDynamicAndTypeField() { Value = item.Value?.Value ?? string.Empty, Key = item.Key, Type = item.Operation });
                    }
                }
            }
        }

        public static void RemoveFiltrItemFromFiltr(this IMessage? protoModel, FiltrItem item)
        {
            if (protoModel == null)
                return;
            FieldDescriptor? field = protoModel.Descriptor.FindFieldByName(item.Key);
            if (field != null)
            {
                if (field.IsRepeated)
                {
                    if (field.MessageType == FiltrValueAndTypeField.Descriptor)
                    {
                        var f = (RepeatedField<FiltrValueAndTypeField>)field.Accessor.GetValue(protoModel);

                        if (f?.Any(x => x.Value.Equals(item.Value?.Value) && x.Type == item.Operation) ?? false)
                        {
                            var first = f.First(x => x.Value.Equals(item.Value?.Value) && x.Type == item.Operation);
                            f.Remove(first);
                        }
                    }
                    else if (field.MessageType == FiltrIntAndTypeField.Descriptor)
                    {
                        var f = (RepeatedField<FiltrIntAndTypeField>)field.Accessor.GetValue(protoModel);

                        var intRange = item.Value?.Value.Split("-") ?? new string[0];
                        int maxValue = 0;
                        int.TryParse(intRange[0], out var minValue);
                        if (item.Operation == FiltrOperationType.Range && intRange.Length > 0)
                        {
                            int.TryParse(intRange[1], out maxValue);
                        }
                        if (f?.Any(x => x.Value == minValue && x.MaxValue == maxValue && x.Type == item.Operation) ?? false)
                        {
                            var first = f.First(x => x.Value == minValue && x.MaxValue == maxValue && x.Type == item.Operation);
                            f.Remove(first);
                        }
                    }
                    else if (field.MessageType == FiltrDataTimeRange.Descriptor)
                    {
                        var f = (RepeatedField<FiltrDataTimeRange>)field.Accessor.GetValue(protoModel);

                        var dateRange = item.Value?.Value.Split("-") ?? new string[0];
                        DateTime? dateEnd = null;
                        DateTime.TryParse(dateRange[0], out var dateStart);
                        if (item.Operation == FiltrOperationType.Range && dateRange.Length > 0)
                        {
                            DateTime.TryParse(dateRange[1], out var _dateEnd);
                            dateEnd = _dateEnd;
                        }
                        if (f?.Any(x => (x.DateStart?.Equals(dateStart.ToUniversalTime().ToTimestamp()) ?? false) && (x.DateEnd?.Equals(dateEnd?.ToUniversalTime().ToTimestamp()) ?? true) && x.Type == item.Operation) ?? false)
                        {
                            var first = f.First(x => (x.DateStart?.Equals(dateStart.ToUniversalTime().ToTimestamp()) ?? false) && (x.DateEnd?.Equals(dateEnd?.ToUniversalTime().ToTimestamp()) ?? true) && x.Type == item.Operation);
                            f.Remove(first);
                        }
                    }
                    else if (field.MessageType == FiltrDataOnlyRange.Descriptor)
                    {
                        var f = (RepeatedField<FiltrDataOnlyRange>)field.Accessor.GetValue(protoModel);

                        var dateRange = item.Value?.Value.Split("-") ?? new string[0];
                        DateTime? dateEnd = null;
                        DateTime.TryParse(dateRange[0], out var dateStart);
                        if (item.Operation == FiltrOperationType.Range && dateRange.Length > 0)
                        {
                            DateTime.TryParse(dateRange[1], out var _dateEnd);
                            dateEnd = _dateEnd;
                        }
                        if (f?.Any(x => (x.DateStart?.Equals(dateStart.ToUniversalTime().ToTimestamp()) ?? false) && (x.DateEnd?.Equals(dateEnd?.ToUniversalTime().ToTimestamp()) ?? true) && x.Type == item.Operation) ?? false)
                        {
                            var first = f.First(x => (x.DateStart?.Equals(dateStart.ToUniversalTime().ToTimestamp()) ?? false) && (x.DateEnd?.Equals(dateEnd?.ToUniversalTime().ToTimestamp()) ?? true) && x.Type == item.Operation);
                            f.Remove(first);
                        }
                    }
                    else if (field.MessageType == FiltrTimeRange.Descriptor)
                    {
                        var f = (RepeatedField<FiltrTimeRange>)field.Accessor.GetValue(protoModel);

                        var timeRange = item.Value?.Value.Split("-") ?? new string[0];
                        TimeSpan? timeEnd = null;
                        TimeSpan.TryParse(timeRange[0], out var timeStart);
                        if (item.Operation == FiltrOperationType.Range && timeRange.Length > 0)
                        {
                            TimeSpan.TryParse(timeRange[1], out var _dateEnd);
                            timeEnd = _dateEnd;
                        }
                        if (f?.Any(x => (x.TimeStart?.ToTimeSpan().Equals(timeStart.LocalTimeSpanToUTC()) ?? false) && (x.TimeEnd?.ToTimeSpan().Equals(timeEnd?.LocalTimeSpanToUTC()) ?? true) && x.Type == item.Operation) ?? false)
                        {
                            var first = f.First(x => (x.TimeStart?.ToTimeSpan().Equals(timeStart.LocalTimeSpanToUTC()) ?? false) && (x.TimeEnd?.ToTimeSpan().Equals(timeEnd?.LocalTimeSpanToUTC()) ?? true) && x.Type == item.Operation);
                            f.Remove(first);
                        }
                    }
                    else if (field.MessageType == FiltrDurationRange.Descriptor)
                    {
                        var f = (RepeatedField<FiltrDurationRange>)field.Accessor.GetValue(protoModel);

                        var timeRange = item.Value?.Value.Split("-") ?? new string[0];
                        TimeSpan? timeEnd = null;
                        TimeSpan.TryParse(timeRange[0], out var timeStart);
                        if (item.Operation == FiltrOperationType.Range && timeRange.Length > 0)
                        {
                            TimeSpan.TryParse(timeRange[1], out var _dateEnd);
                            timeEnd = _dateEnd;
                        }
                        if (f?.Any(x => (x.Start?.ToTimeSpan().Equals(timeStart) ?? false) && (x.End?.ToTimeSpan().Equals(timeEnd) ?? true) && x.Type == item.Operation) ?? false)
                        {
                            var first = f.First(x => (x.Start?.ToTimeSpan().Equals(timeStart) ?? false) && (x.End?.ToTimeSpan().Equals(timeEnd) ?? true) && x.Type == item.Operation);
                            f.Remove(first);
                        }
                    }
                }
                else
                {
                    if (field.MessageType == FiltrBoolField.Descriptor)
                    {
                        field.Accessor.SetValue(protoModel, null);
                    }
                }
            }
            else
            {
                field = protoModel.Descriptor.Fields.InDeclarationOrder().FirstOrDefault(x => x.MessageType == FiltrDynamicAndTypeField.Descriptor);

                if (field?.IsRepeated ?? false)
                {
                    var f = (RepeatedField<FiltrDynamicAndTypeField>)field.Accessor.GetValue(protoModel);

                    if (f?.Any(x => x.Key == item.Key && x.Value.Equals(item.Value?.Value) && x.Type == item.Operation) ?? false)
                    {
                        var first = f.First(x => x.Key == item.Key && x.Value.Equals(item.Value?.Value) && x.Type == item.Operation);
                        f.Remove(first);
                    }
                }
            }

        }


        public static IEnumerable<FiltrItem> CreateListFiltrItemFromFiltrModel(this IMessage? protoModel)
        {
            List<FiltrItem> response = new();
            if (protoModel != null)
            {
                var fields = protoModel.Descriptor.Fields?.InDeclarationOrder();

                if (fields != null)
                {
                    foreach (var field in fields)
                    {
                        if (!field.IsRepeated)
                        {
                            if (field.MessageType == FiltrBoolField.Descriptor)
                            {
                                var f = (FiltrBoolField)field.Accessor.GetValue(protoModel);
                                if (f != null)
                                    response.Add(new FiltrItem(field.PropertyName, new(f.ViewName, f.Value.ToString()), FiltrOperationType.BoolEqual));
                            }
                        }
                        else
                        {
                            if (field.MessageType == FiltrValueAndTypeField.Descriptor)
                            {
                                var f = (RepeatedField<FiltrValueAndTypeField>)field.Accessor.GetValue(protoModel);
                                response.AddRange(f.Select(x => new FiltrItem(field.PropertyName, new(x.Value, x.KeyValue), x.Type)));
                            }
                            else if (field.MessageType == FiltrIntAndTypeField.Descriptor)
                            {
                                var f = (RepeatedField<FiltrIntAndTypeField>)field.Accessor.GetValue(protoModel);
                                response.AddRange(f.Select(x => new FiltrItem(field.PropertyName, new(x.Type == FiltrOperationType.Range ? $"{x.Value}-{x.MaxValue}" : $"{x.Value}"), x.Type)));
                            }
                            else if (field.MessageType == FiltrDataTimeRange.Descriptor)
                            {
                                var f = (RepeatedField<FiltrDataTimeRange>)field.Accessor.GetValue(protoModel);
                                response.AddRange(f.Select(x => new FiltrItem(field.PropertyName, new(x.Type == FiltrOperationType.Range ? $"{x.DateStart?.ToDateTime().ToLocalTime().ToString("g")}-{x.DateEnd?.ToDateTime().ToLocalTime().ToString("g")}" : $"{x.DateStart?.ToDateTime().ToLocalTime().ToString("g")}"), x.Type)));
                            }
                            else if (field.MessageType == FiltrDataOnlyRange.Descriptor)
                            {
                                var f = (RepeatedField<FiltrDataOnlyRange>)field.Accessor.GetValue(protoModel);
                                response.AddRange(f.Select(x => new FiltrItem(field.PropertyName, new(x.Type == FiltrOperationType.Range ? $"{x.DateStart?.ToDateTime().ToLocalTime().ToString("d")}-{x.DateEnd?.ToDateTime().ToLocalTime().ToString("d")}" : $"{x.DateStart?.ToDateTime().ToLocalTime().ToString("d")}"), x.Type)));
                            }
                            else if (field.MessageType == FiltrTimeRange.Descriptor)
                            {
                                var f = (RepeatedField<FiltrTimeRange>)field.Accessor.GetValue(protoModel);
                                response.AddRange(f.Select(x => new FiltrItem(field.PropertyName, new(x.Type == FiltrOperationType.Range ? $"{x.TimeStart?.ToTimeSpan().UTCTimeSpanToLocal().ToString("hh\\:mm")}-{x.TimeEnd?.ToTimeSpan().UTCTimeSpanToLocal().ToString("hh\\:mm")}" : $"{x.TimeStart?.ToTimeSpan().UTCTimeSpanToLocal().ToString("hh\\:mm")}"), x.Type)));
                            }
                            else if (field.MessageType == FiltrDurationRange.Descriptor)
                            {
                                var f = (RepeatedField<FiltrDurationRange>)field.Accessor.GetValue(protoModel);
                                response.AddRange(f.Select(x => new FiltrItem(field.PropertyName, new(x.Type == FiltrOperationType.Range ? $"{x.Start?.ToTimeSpan().ToString("dd\\.hh\\:mm")}-{x.End?.ToTimeSpan().ToString("dd\\.hh\\:mm")}" : $"{x.Start?.ToTimeSpan().ToString("dd\\.hh\\:mm")}"), x.Type)));
                            }
                            else if (field.MessageType == FiltrDynamicAndTypeField.Descriptor)
                            {
                                var f = (RepeatedField<FiltrDynamicAndTypeField>)field.Accessor.GetValue(protoModel);
                                foreach (var item in f.GroupBy(x => x.Key))
                                {
                                    response.AddRange(item.Select(x => new FiltrItem(item.First().Key, new(x.Value), x.Type)));
                                }
                            }
                        }
                    }
                }
            }
            return response;
        }
    }
}
