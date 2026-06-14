using Autofac;
using CompanyName.MyMeetings.Modules.UserAccess.Application.Contracts;

namespace CompanyName.MyMeetings.Modules.Registrations.Infrastructure.Configuration.UserAccess
{
    public class UserAccessAutofacModule : Module
    {
        private readonly IUserAccessModule _userAccessModule;

        public UserAccessAutofacModule(IUserAccessModule userAccessModule)
        {
            _userAccessModule = userAccessModule;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_userAccessModule).As<IUserAccessModule>();
        }
    }
}