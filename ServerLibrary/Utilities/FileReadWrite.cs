using System.Text;
using System.Text.Json;

namespace ServerLibrary.Utilities
{
    public class FileReadWrite
    {
        public async Task WriteText<TData>(string SavePath, TData Content)
        {
            try
            {
                string p = Path.Combine("wwwroot", SavePath) ?? "";

                string? dir = Path.GetDirectoryName(p);

                if (!Directory.Exists(dir) && !string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string bodyString = JsonSerializer.Serialize(Content);
                bodyString = Convert.ToBase64String(Encoding.UTF8.GetBytes(bodyString));

                int Count = 0;

                while (true)
                {
                    try
                    {
                        await System.IO.File.WriteAllTextAsync(p, bodyString);
                        break;
                    }
                    catch
                    {
                        await Task.Delay(100);
                    }

                    if (Count > 5)
                        break;
                    Count++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        public async Task<TData?> ReadFile<TData>(string ReadPath)
        {
            TData? request = default;
            try
            {
                string p = Path.Combine("wwwroot", ReadPath) ?? "";
                string bodyString = "";
                int Count = 0;
                if (System.IO.File.Exists(p))
                {
                    while (true)
                    {
                        try
                        {
                            bodyString = await System.IO.File.ReadAllTextAsync(p);
                            break;
                        }
                        catch
                        {
                            await Task.Delay(100);
                        }

                        if (Count > 5)
                            break;
                        Count++;
                    }

                    if (!string.IsNullOrEmpty(bodyString))
                    {
                        bodyString = Encoding.UTF8.GetString(Convert.FromBase64String(bodyString));
                        request = JsonSerializer.Deserialize<TData>(bodyString);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return request;
        }
    }
}
