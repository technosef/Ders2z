using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using DersDagitim.Domain;
using DersDagitim.Application;

namespace DersDagitim.Wpf;
public partial class SchoolOverviewWindow : Window
{
    private IReadOnlyList<SchoolClass> _classes = Array.Empty<SchoolClass>();
    private IReadOnlyList<Teacher> _teachers = Array.Empty<Teacher>();
    private IReadOnlyList<Course> _courses = Array.Empty<Course>();
    private IReadOnlyList<Resource> _resources = Array.Empty<Resource>();
    
    public SchoolOverviewWindow() 
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDataAsync();
    }
    
    private async Task LoadDataAsync()
    {
        _classes = await App.Dashboard.Repository.GetClassesAsync();
        _teachers = await App.Dashboard.Repository.GetTeachersAsync();
        _courses = await App.Dashboard.Repository.GetCoursesAsync();
        _resources = await App.Dashboard.Repository.GetResourcesAsync();
        
        // Demo verileri + gerçek veriler
        var rows = new List<OverviewRow>();
        
        // Gerçek verilerden özet
        rows.Add(new OverviewRow("Genel", "-", "-", "-", $"{_classes.Count} sınıf · {_teachers.Count} öğretmen · {_courses.Count} ders", "İstatistik"));
        
        // Sınıf bazlı özet
        foreach (var schoolClass in _classes.OrderBy(c => c.Grade).ThenBy(c => c.Branch))
        {
            var classCourses = _courses.Where(c => true).Take(3).ToList(); // Demo: ilk 3 ders
            rows.Add(new OverviewRow("-", schoolClass.Name, "-", schoolClass.Department ?? "-", 
                $"{classCourses.Count} ders · {schoolClass.ProgramType}", "Sınıf"));
        }
        
        // Öğretmen bazlı özet
        foreach (var teacher in _teachers.OrderBy(t => t.FullName).Take(5))
        {
            rows.Add(new OverviewRow("-", teacher.FullName, "-", teacher.Department ?? "-", 
                $"{teacher.WeeklyMaximumHours} saat · {teacher.ColorCode}", "Öğretmen"));
        }
        
        OverviewGrid.ItemsSource = rows;
    }
    
    private sealed record OverviewRow(string Day, string Group, string Slot, string Department, string Display, string Status);
}
