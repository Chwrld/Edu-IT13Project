using MauiAppIT13.Pages.Student;
using MauiAppIT13.Pages.Admin;
using MauiAppIT13.Pages.Teacher;

namespace MauiAppIT13
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
            // Register routes for navigation
            Routing.RegisterRoute("HomePage", typeof(HomePage));
            Routing.RegisterRoute("AdminLoginPage", typeof(AdminLoginPage));
            Routing.RegisterRoute("AdminHomePage", typeof(AdminHomePage));
            Routing.RegisterRoute("TeacherLoginPage", typeof(TeacherLoginPage));
            Routing.RegisterRoute("TeacherHomePage", typeof(TeacherHomePage));
            Routing.RegisterRoute("MainPage", typeof(MainPage));
        }
    }
}
