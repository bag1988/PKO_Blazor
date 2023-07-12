using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FiltersGSOProto.V1;

namespace BlazorLibrary.Models
{
    public record class FiltrItem
    {
        public FiltrItem(string key, Hint? value = null, FiltrOperationType operation = FiltrOperationType.None)
        {
            Value = value;
            Operation = operation;
            Key = key;
        }
       
        public string Key { get; set; }
        public FiltrOperationType Operation { get; set; }
        public Hint? Value { get; set; }
    }


    public enum TypeHint
    {
        Input = 1,
        Select,
        OnlySelect,
        Number,
        Date,
        Time,
        DateOnly,
        Bool,
        Duration,
        ContainsOnly
    }

    public record class Hint
    {
        public Hint(string value, string? keyValue = null)
        {
            Value = value;
            KeyValue = keyValue;
        }
        public string? KeyValue { get; set; }
        public string Value { get; set; }
    }

    public record class HintItem : FiltrItem
    {
        public HintItem(string key, string name, TypeHint type, Hint? value = null, FiltrOperationType operation = FiltrOperationType.None, VirtualizeProvider<Hint>? provider = null)
            : base(key, value, operation)
        {
            Name = name;
            Type = type;
            Provider = provider;
        }

        public TypeHint Type { get; set; }
        public string Name { get; set; }
        public VirtualizeProvider<Hint>? Provider { get; set; }
    }
}
