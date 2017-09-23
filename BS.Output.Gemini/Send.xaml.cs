using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace BS.Output.Gemini
{
  partial class Send : Window
  {
 
    public Send(string url, string lastProjectID, string lastIssueID, string fileName)
    {
      InitializeComponent();
      
      // TODO
      //List<ProjectItem> projectItems = new List<ProjectItem>();
      //InitProjects(projectItems, projects, String.Empty);
      //ProjectComboBox.ItemsSource = projectItems;

      Url.Text = url;
      NewIssue.IsChecked = true;
      ProjectComboBox.SelectedValue = lastProjectID;
      IssueIDTextBox.Text = lastIssueID;
      FileNameTextBox.Text = fileName;

      ProjectComboBox.SelectionChanged += ValidateData;
      SummaryTextBox.TextChanged += ValidateData;
      DescriptionTextBox.TextChanged += ValidateData;
      IssueIDTextBox.TextChanged += ValidateData;
      FileNameTextBox.TextChanged += ValidateData;
      ValidateData(null, null);

    }

    public bool CreateNewIssue
    {
      get { return NewIssue.IsChecked.Value; }
    }
 
    public int ProjectID
    {
      get { return (int)ProjectComboBox.SelectedValue; }
    }
      
    public string Summary
    {
      get { return SummaryTextBox.Text; }
    }

    public string Description
    {
      get { return DescriptionTextBox.Text; }
    }

    public int IssueID
    {
      get { return Convert.ToInt32(IssueIDTextBox.Text); }
    }

    public string FileName
    {
      get { return FileNameTextBox.Text; }
    }

    private void NewIssue_CheckedChanged(object sender, EventArgs e)
    {

      if (NewIssue.IsChecked.Value)
      {
        ProjectControls.Visibility = Visibility.Visible;
        SummaryControls.Visibility = Visibility.Visible;
        DescriptionControls.Visibility = Visibility.Visible;
        IssueIDControls.Visibility = Visibility.Collapsed;

        SummaryTextBox.SelectAll();
        SummaryTextBox.Focus();
      }
      else
      {
        ProjectControls.Visibility = Visibility.Collapsed;
        SummaryControls.Visibility = Visibility.Collapsed;
        DescriptionControls.Visibility = Visibility.Collapsed;
        IssueIDControls.Visibility = Visibility.Visible;
        
        IssueIDTextBox.SelectAll();
        IssueIDTextBox.Focus();
      }

      ValidateData(null, null);

    }

    private void IssueID_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
    }
    
    private void ValidateData(object sender, EventArgs e)
    {
      OK.IsEnabled = ((CreateNewIssue && Validation.IsValid(ProjectComboBox) && Validation.IsValid(SummaryTextBox) && Validation.IsValid(DescriptionTextBox)) ||
                      (!CreateNewIssue && Validation.IsValid(IssueIDTextBox))) &&
                     Validation.IsValid(FileNameTextBox);
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
      this.DialogResult = true;
    }

  }

  internal class ProjectItem
  {
    
    private string projectID;
    private string fullName;

    public ProjectItem(string projectID, string fullName)
    {
      this.projectID = projectID;
      this.fullName = fullName;
    }

    public string ProjectID
    {
      get { return projectID; }
    }

    public override string ToString()
    {
      return fullName;
    }

  }

}
