using System;

namespace Justin.Updater.Server
{
    static class EmpHelper
    {
        public static LoginEmpInfo Login(string usr, string password)
        {
            var user = AppSettingHelper.User;
            var pwd = AppSettingHelper.Password;

            password = CryptoHelper.MD5(password);

            if (user != usr || password != pwd)
            {
                throw new Exception("用户名或密码错误");
            }

            return new LoginEmpInfo
            {
                Id = 1,
                Code = usr,
                Name = "Sps管理员",
                Password = pwd
            };
        }
    }
}