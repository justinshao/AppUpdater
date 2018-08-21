using System.IO;

namespace Justin.Updater.Client
{
    static class IOHelper
    {
        public static void DeleteFile(string file)
        {
            if (File.Exists(file))
            {
                //TODO: 删除文件后如果文件夹为空，尝试删除文件夹
                //var info = new FileInfo(file);
                
                File.Delete(file);
            }
        }
        public static Stream Create(string file)
        {
            var info = new FileInfo(file);
            if (!Directory.Exists(info.DirectoryName))
            {
                Directory.CreateDirectory(info.DirectoryName);
            }

            return info.Create();
        }
        public static void MoveFile(string src, string dst)
        {
            var dstInfo = new FileInfo(dst);

            if (dstInfo.Exists)
                dstInfo.Delete();
            else if (Directory.Exists(dstInfo.DirectoryName))
                Directory.CreateDirectory(dstInfo.DirectoryName);

            File.Move(src, dstInfo.FullName);
        }

        public static void WriteFilePossibllyInUse(string dst, Stream data, bool throwOnError = false)
        {
            var tmp = dst + ".$tmp";
            using (var fs = File.Create(tmp))
            {
                data.CopyTo(fs);
            }

            var old = dst + ".$old";
            try
            {
                MoveFile(dst, old);
                MoveFile(tmp, dst);
            }
            catch
            {
                if(throwOnError)
                    throw;
            }

            try
            {
                DeleteFile(old);
            }
            catch { }
        }
    }
}