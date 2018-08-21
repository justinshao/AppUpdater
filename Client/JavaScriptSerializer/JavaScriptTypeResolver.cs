namespace Justin.Serialization.Json
{
    using System;
    using System.Security.Permissions;
    using System.Web;

    [AspNetHostingPermission(SecurityAction.LinkDemand, Level=AspNetHostingPermissionLevel.Minimal), AspNetHostingPermission(SecurityAction.InheritanceDemand, Level=AspNetHostingPermissionLevel.Minimal)]
    public abstract class JavaScriptTypeResolver
    {
        protected JavaScriptTypeResolver()
        {
        }

        public abstract Type ResolveType(string id);
        public abstract string ResolveTypeId(Type type);
    }
}

