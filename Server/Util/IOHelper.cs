using System.IO;

namespace Justin.Updater.Server
{
    static class IOHelper
    {
        public static DirCreateResult EnsureCreateDir(string dir)
        {
            var ret = new DirCreateResult
            {
                Exists = Directory.Exists(dir),
                Dir = dir,
            };

            if (!ret.Exists)
            {
                Directory.CreateDirectory(dir);
            }

            return ret;
        }
        public static void MoveFile(string src, string dst)
        {
            var dstInfo = new FileInfo(dst);

            if (dstInfo.Exists)
                dstInfo.Delete();
            else
                EnsureCreateDir(dstInfo.DirectoryName);
            
            File.Move(src, dstInfo.FullName);
        }
        public static void DeleteFile(string file)
        {
            if (File.Exists(file))
            {
                //TODO: 删除文件后如果文件夹为空，尝试删除文件夹
                //var info = new FileInfo(file);

                File.Delete(file);
            }
        }
        public static void DeleteDir(string dir)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
        public static void CreateEmptyFile(string file)
        {
            var fileInfo = new FileInfo(file);

            var existsDir = false;

            try
            {
                existsDir = EnsureCreateDir(fileInfo.DirectoryName).Exists;
                using (File.Create(file)) { }
            }
            catch
            {
                if(!existsDir)
                {
                    DeleteDir(fileInfo.DirectoryName);
                }

                throw;
            }

        }
    }

    struct DirCreateResult
    {
        public bool Exists { get; set; }
        public string Dir { get; set; }
    }
}