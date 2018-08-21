namespace Justin.Updater.Server.Models.Home
{
    public class SystemEditViewModel
    {
        /// <summary>
        /// 1: 新增， 2:修改
        /// </summary>
        public int Mode { get; set; }

        public int SystemId { get; set; }
        public string Name { get; set; }
    }
}