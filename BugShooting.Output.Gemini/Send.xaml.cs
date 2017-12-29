using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Countersoft.Gemini.Commons.Dto;

namespace BugShooting.Output.Gemini
{
  partial class Send : Window
  {

    List<IssueTypeDto> issueTypes;

    public Send(string url, int lastProjectID, int lastIssueTypeID, int lastIssueID, List<ProjectDto> projects, List<IssueTypeDto> issueTypes, string fileName)
    {
      InitializeComponent();

      this.issueTypes = issueTypes;

      List<ProjectItem> projectItems = new List<ProjectItem>();
      foreach (ProjectDto project in projects)
      {
        projectItems.Add(new ProjectItem(project.Entity.Id, project.Label, project.Entity.WorkflowId));
      }
      ProjectComboBox.ItemsSource = projectItems;

      Url.Text = url;
      NewIssue.IsChecked = true;
      ProjectComboBox.SelectedValue = lastProjectID;

      if (ProjectComboBox.SelectedValue != null)
      {
        IssueTypeComboBox.SelectedValue = lastIssueTypeID;
      }
          
      IssueIDTextBox.Text = lastIssueID.ToString();
      FileNameTextBox.Text = fileName;

      ProjectComboBox.SelectionChanged += ValidateData;
      TitleTextBox.TextChanged += ValidateData;
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

    public int IssueTypeID
    {
      get { return (int)IssueTypeComboBox.SelectedValue; }
    }

    public string IssueTitle
    {
      get { return TitleTextBox.Text; }
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

        TitleTextBox.SelectAll();
        TitleTextBox.Focus();
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

    private void ProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

      if (ProjectComboBox.SelectedItem is null)
      {
        IssueTypeComboBox.ItemsSource = null;
      }
      else
      {

        int workflowId = ((ProjectItem)ProjectComboBox.SelectedItem).WorkflowId;
        
        List<ItemTypeItem> itemTypeItems = new List<ItemTypeItem>();
        foreach (IssueTypeDto itemType in issueTypes)
        {
          if (itemType.Entity.Workflow.ReferenceId == workflowId)
          {
            itemTypeItems.Add(new ItemTypeItem(itemType.Entity.Id, itemType.Entity.Label));
          }
        }
        IssueTypeComboBox.ItemsSource = itemTypeItems;

      }

    }

    private void IssueID_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
    }
    
    private void ValidateData(object sender, EventArgs e)
    {
      OK.IsEnabled = ((CreateNewIssue && Validation.IsValid(ProjectComboBox) && Validation.IsValid(IssueTypeComboBox) && Validation.IsValid(TitleTextBox) && Validation.IsValid(DescriptionTextBox)) ||
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
    
    private int id;
    private string name;
    private int workflowId;

    public ProjectItem(int id, string name, int workflowId)
    {
      this.id = id;
      this.name = name;
      this.workflowId = workflowId;
    }

    public int Id
    {
      get { return id; }
    }

    public string Name
    {
      get { return name; }
    }

    public int WorkflowId
    {
      get { return workflowId; }
    }

  }

  internal class ItemTypeItem
  {

    private int id;
    private string name;
    public ItemTypeItem(int id, string name)
    {
      this.id = id;
      this.name = name;
    }

    public int Id
    {
      get { return id; }
    }

    public string Name
    {
      get { return name; }
    }
    
  }

}
