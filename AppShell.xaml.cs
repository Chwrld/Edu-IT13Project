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
            Routing.RegisterRoute("AdminHomePage", typeof(AdminHomePage));
            Routing.RegisterRoute("AdminUsersPage", typeof(AdminUsersPage));
            Routing.RegisterRoute("AdminTicketsPage", typeof(AdminTicketsPage));
            Routing.RegisterRoute("AdminAnnouncementsPage", typeof(AdminAnnouncementsPage));
            Routing.RegisterRoute("AdminReportsPage", typeof(AdminReportsPage));
            Routing.RegisterRoute("TeacherHomePage", typeof(TeacherHomePage));
            Routing.RegisterRoute("MainPage", typeof(MainPage));
        }
    }
}
