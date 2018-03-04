using BS.Plugin.V3.Common;
using BS.Plugin.V3.Output;
using BS.Plugin.V3.Utilities;
using Countersoft.Gemini.Api;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini.Commons.Entity;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BugShooting.Output.Gemini
{
  public class OutputPlugin: OutputPlugin<Output>
  {

    protected override string Name
    {
      get { return "Gemini"; }
    }

    protected override Image Image64
    {
      get  { return Properties.Resources.logo_64; }
    }

    protected override Image Image16
    {
      get { return Properties.Resources.logo_16 ; }
    }

    protected override bool Editable
    {
      get { return true; }
    }

    protected override string Description
    {
      get { return "Attach screenshots to Gemini issues."; }
    }
    
    protected override Output CreateOutput(IWin32Window Owner)
    {
      
      Output output = new Output(Name, 
                                 String.Empty,
                                 false,
                                 String.Empty, 
                                 String.Empty, 
                                 "Screenshot",
                                 FileHelper.GetFileFormats().First().ID,
                                 true,
                                 1,
                                 1,
                                 1);

      return EditOutput(Owner, output);

    }

    protected override Output EditOutput(IWin32Window Owner, Output Output)
    {

      Edit edit = new Edit(Output);

      var ownerHelper = new System.Windows.Interop.WindowInteropHelper(edit);
      ownerHelper.Owner = Owner.Handle;
      
      if (edit.ShowDialog() == true) {

        return new Output(edit.OutputName,
                          edit.Url,
                          edit.IntegratedAuthentication,
                          edit.UserName,
                          edit.Password,
                          edit.FileName,
                          edit.FileFormatID,
                          edit.OpenItemInBrowser,
                          Output.LastProjectID,
                          Output.LastIssueTypeID,
                          Output.LastIssueID);
      }
      else
      {
        return null; 
      }

    }

    protected override OutputValues SerializeOutput(Output Output)
    {

      OutputValues outputValues = new OutputValues();

      outputValues.Add("Name", Output.Name);
      outputValues.Add("Url", Output.Url);
      outputValues.Add("IntegratedAuthentication", Convert.ToString(Output.IntegratedAuthentication));
      outputValues.Add("UserName", Output.UserName);
      outputValues.Add("Password",Output.Password, true);
      outputValues.Add("OpenItemInBrowser", Convert.ToString(Output.OpenItemInBrowser));
      outputValues.Add("FileName", Output.FileName);
      outputValues.Add("FileFormatID", Output.FileFormatID.ToString());
      outputValues.Add("LastProjectID", Output.LastProjectID.ToString());
      outputValues.Add("LastIssueTypeID", Output.LastIssueTypeID.ToString());
      outputValues.Add("LastIssueID", Output.LastIssueID.ToString());

      return outputValues;
      
    }

    protected override Output DeserializeOutput(OutputValues OutputValues)
    {

      return new Output(OutputValues["Name", this.Name],
                        OutputValues["Url", ""],
                        Convert.ToBoolean(OutputValues["IntegratedAuthentication", Convert.ToString(true)]),
                        OutputValues["UserName", ""],
                        OutputValues["Password", ""],
                        OutputValues["FileName", "Screenshot"],
                        new Guid(OutputValues["FileFormatID", ""]),
                        Convert.ToBoolean(OutputValues["OpenItemInBrowser", Convert.ToString(true)]),
                        Convert.ToInt32(OutputValues["LastProjectID", "1"]),
                        Convert.ToInt32(OutputValues["LastIssueTypeID", "1"]),
                        Convert.ToInt32(OutputValues["LastIssueID", "1"]));

    }

    protected override async Task<SendResult> Send(IWin32Window Owner, Output Output, ImageData ImageData)
    {

      try
      {

        bool integratedAuthentication = Output.IntegratedAuthentication;
        string userName = Output.UserName;
        string password = Output.Password;
        bool showLogin = !integratedAuthentication && (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password));
        bool rememberCredentials = false;

        string fileName = AttributeHelper.ReplaceAttributes(Output.FileName, ImageData);
       
        while (true)
        {

          if (showLogin)
          {

            // Show credentials window
            Credentials credentials = new Credentials(Output.Url, userName, password, rememberCredentials);

            var credentialsOwnerHelper = new System.Windows.Interop.WindowInteropHelper(credentials);
            credentialsOwnerHelper.Owner = Owner.Handle;

            if (credentials.ShowDialog() != true)
            {
              return new SendResult(Result.Canceled);
            }

            userName = credentials.UserName;
            password = credentials.Password;
            rememberCredentials = credentials.Remember;

          }

          ServiceManager gemini;
          if (integratedAuthentication)
          {
            gemini = new ServiceManager(Output.Url);
          }
          else
          {
            gemini = new ServiceManager(Output.Url, userName, password, string.Empty);
          }
                  
          try
          {
            
            // Get active projects
            List<ProjectDto> allProjects = await Task.Factory.StartNew(() => gemini.Projects.GetProjects());
            List<ProjectDto> projects = new List<ProjectDto>();
            foreach (ProjectDto project in allProjects)
            {
              if (!project.Entity.Archived)
              {
                projects.Add(project);
              }
            }

            // Get issue types
            List<IssueTypeDto> issueTypes = await Task.Factory.StartNew(() => gemini.Meta.GetIssueTypes());
           
            // Show send window
            Send send = new Send(Output.Url, Output.LastProjectID, Output.LastIssueTypeID, Output.LastIssueID, projects, issueTypes, fileName);

            var sendOwnerHelper = new System.Windows.Interop.WindowInteropHelper(send);
            sendOwnerHelper.Owner = Owner.Handle;

            if (!send.ShowDialog() == true)
            {
              return new SendResult(Result.Canceled);
            }

            IFileFormat fileFormat = FileHelper.GetFileFormat(Output.FileFormatID);

            string fullFileName = String.Format("{0}.{1}", send.FileName, fileFormat.FileExtension);

            byte[] fileBytes = FileHelper.GetFileBytes(Output.FileFormatID, ImageData);

            int projectID;
            int issueTypeID;
            int issueID;

            if (send.CreateNewIssue)
            {

              projectID = send.ProjectID;
              issueTypeID = send.IssueTypeID;

              UserDto user = await Task.Factory.StartNew(() => gemini.User.WhoAmI());

              Issue issue = new Issue();
              issue.ProjectId = projectID;
              issue.TypeId = issueTypeID;
              issue.Title = send.IssueTitle;
              issue.Description = send.Description;
              issue.ReportedBy = user.Entity.Id;

              IssueAttachment attachment = new IssueAttachment();
              attachment.ContentLength = fileBytes.Length;
              attachment.ContentType = fileFormat.MimeType;
              attachment.Content = fileBytes;
              attachment.Name = fullFileName;
              issue.Attachments.Add(attachment);

              IssueDto createdIssue = await Task.Factory.StartNew(() => gemini.Item.Create(issue));
           
              issueID = createdIssue.Id;
        
            }
            else
            {

              issueID = send.IssueID;

              IssueDto issue = await Task.Factory.StartNew(() => gemini.Item.Get(issueID));

              projectID = issue.Project.Id;
              issueTypeID = Output.LastIssueTypeID;

              IssueAttachment attachment = new IssueAttachment();
              attachment.ProjectId = projectID;
              attachment.IssueId = issueID;
              attachment.ContentLength = fileBytes.Length;
              attachment.ContentType = fileFormat.MimeType;
              attachment.Content = fileBytes;
              attachment.Name = fullFileName;
        
              await Task.Factory.StartNew(() => gemini.Item.IssueAttachmentCreate(attachment));

            }

        
            // Open issue in browser
            if (Output.OpenItemInBrowser)
            {
              WebHelper.OpenUrl(String.Format("{0}/workspace/{1}/item/{2}", Output.Url, projectID, issueID));
            }

            return new SendResult(Result.Success,
                                  new Output(Output.Name,
                                             Output.Url,
                                             (rememberCredentials) ? false : Output.IntegratedAuthentication,
                                             (rememberCredentials) ? userName : Output.UserName,
                                             (rememberCredentials) ? password : Output.Password,
                                             Output.FileName,
                                             Output.FileFormatID,
                                             Output.OpenItemInBrowser,
                                             projectID,
                                             issueTypeID,
                                             issueID));

          }
          catch (RestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
          {
            // Login failed
            integratedAuthentication = false;
            showLogin = true;
            continue;
          }
          
        }

      }
      catch (Exception ex)
      {
        return new SendResult(Result.Failed, ex.Message);
      }

    }

  }
}
