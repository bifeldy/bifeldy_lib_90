using bifeldy_lib_90.Models;

namespace bifeldy_lib_90.Attributes {

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AllowedRolesAttribute(params ESessionRole[] role) : Attribute {
        public ESessionRole[] Roles { get; } = role;
    }

}