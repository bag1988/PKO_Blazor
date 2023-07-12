using Google.Protobuf.WellKnownTypes;

namespace BlazorLibrary
{
    public static class SortList
    {
        //for protobuf model
        public static void Sort<Tdata>(ref List<Tdata>? model, int columnNumber = 0, int flagSort = 0)
        {
            try
            {
                if (model == null)
                    return;

                bool flag = flagSort == 1;

                if (model.FirstOrDefault()?.GetType().GetProperties().Where(x => x.CanWrite).Count() >= columnNumber)
                {

                    var prop = model.FirstOrDefault()?.GetType().GetProperties().Where(x => x.CanWrite).ToList();

                    var p = prop?.ElementAt(columnNumber);

                    if (p?.PropertyType.Equals(new Timestamp().GetType()) ?? false)
                    {
                        if (flag)
                            model = model.OrderBy(x => p?.GetValue(x, null) as Timestamp).ToList();
                        else
                            model = model.OrderByDescending(x => p?.GetValue(x, null) as Timestamp).ToList();
                    }
                    else
                        model = model.OrderBy(x => (flag ? p?.GetValue(x, null) : true)).ThenByDescending(x => (!flag ? p?.GetValue(x, null) : true)).ToList();
                }
            }
            catch
            {

            }

        }
    }
}
