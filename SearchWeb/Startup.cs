using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SearchWeb.Startup))]
namespace SearchWeb
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
