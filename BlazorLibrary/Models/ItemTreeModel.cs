using SMDataServiceProto.V1;

namespace BlazorLibrary.Models
{
    public class ItemTree
    {
        public Objects Key { get; set; } = new();
        public List<CGetSitItemInfo> Child { get; set; } = new();
    }

    public class ListType
    {
        public const int Person = 0;
        public const int Aso = 1;
        public const int Szs = 2;
        public const int Staff = 3;
        public const int MEN = 4;
        public const int LIST = 5;
        public const int DEPARTMENT = 6;
        public const int MAN = 7;
    }


    public class ChildItems<TItem>
    {
        public ChildItems(TItem key, Func<TItem, IEnumerable<ChildItems<TItem>>?>? getChild = null)
        {
            Key = key;            
            GetChild = getChild;
            if (getChild != null)
                IsContainsChild = true;
        }
        public TItem Key { get; init; }
        public bool IsContainsChild { get; init; } = false;
        readonly Func<TItem, IEnumerable<ChildItems<TItem>>?>? GetChild;
        public IEnumerable<ChildItems<TItem>>? Childs
        {
            get
            {
                if (Key != null && GetChild != null)
                {
                    return GetChild(Key);
                }
                return null;
            }
        }
    }
}
