using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Linq;
using Countersoft.Gemini.Api;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini.Commons.Entity;

namespace BS.Output.Gemini
{
  public class OutputAddIn: V3.OutputAddIn<Output>
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
                                 String.Empty, 
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
                          edit.FileFormat,
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

    protected override OutputValueCollection SerializeOutput(Output Output)
    {

      OutputValueCollection outputValues = new OutputValueCollection();

      outputValues.Add(new OutputValue("Name", Output.Name));
      outputValues.Add(new OutputValue("Url", Output.Url));
      outputValues.Add(new OutputValue("IntegratedAuthentication", Convert.ToString(Output.IntegratedAuthentication)));
      outputValues.Add(new OutputValue("UserName", Output.UserName));
      outputValues.Add(new OutputValue("Password",Output.Password, true));
      outputValues.Add(new OutputValue("OpenItemInBrowser", Convert.ToString(Output.OpenItemInBrowser)));
      outputValues.Add(new OutputValue("FileName", Output.FileName));
      outputValues.Add(new OutputValue("FileFormat", Output.FileFormat));
      outputValues.Add(new OutputValue("LastProjectID", Output.LastProjectID.ToString()));
      outputValues.Add(new OutputValue("LastIssueTypeID", Output.LastIssueTypeID.ToString()));
      outputValues.Add(new OutputValue("LastIssueID", Output.LastIssueID.ToString()));

      return outputValues;
      
    }

    protected override Output DeserializeOutput(OutputValueCollection OutputValues)
    {

      return new Output(OutputValues["Name", this.Name].Value,
                        OutputValues["Url", ""].Value,
                        Convert.ToBoolean(OutputValues["IntegratedAuthentication", Convert.ToString(true)].Value),
                        OutputValues["UserName", ""].Value,
                        OutputValues["Password", ""].Value,
                        OutputValues["FileName", "Screenshot"].Value, 
                        OutputValues["FileFormat", ""].Value,
                        Convert.ToBoolean(OutputValues["OpenItemInBrowser", Convert.ToString(true)].Value),
                        Convert.ToInt32(OutputValues["LastProjectID", "1"].Value),
                        Convert.ToInt32(OutputValues["LastIssueTypeID", "1"].Value),
                        Convert.ToInt32(OutputValues["LastIssueID", "1"].Value));

    }

    protected override async Task<V3.SendResult> Send(IWin32Window Owner, Output Output, V3.ImageData ImageData)
    {

      try
      {

        bool integratedAuthentication = Output.IntegratedAuthentication;
        string userName = Output.UserName;
        string password = Output.Password;
        bool showLogin = !integratedAuthentication && (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password));
        bool rememberCredentials = false;

        string fileName = V3.FileHelper.GetFileName(Output.FileName, Output.FileFormat, ImageData);
       
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
              return new V3.SendResult(V3.Result.Canceled);
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
              return new V3.SendResult(V3.Result.Canceled);
            }

            string fullFileName = String.Format("{0}.{1}", send.FileName, V3.FileHelper.GetFileExtention(Output.FileFormat));
            string fileMimeType = V3.FileHelper.GetMimeType(Output.FileFormat);
            byte[] fileBytes = V3.FileHelper.GetFileBytes(Output.FileFormat, ImageData);

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
              attachment.ContentType = fileMimeType;
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
              attachment.ContentType = fileMimeType;
              attachment.Content = fileBytes;
              attachment.Name = fullFileName;
        
              await Task.Factory.StartNew(() => gemini.Item.IssueAttachmentCreate(attachment));

            }

        
            // Open issue in browser
            if (Output.OpenItemInBrowser)
            {
              V3.WebHelper.OpenUrl(String.Format("{0}/workspace/{1}/item/{2}", Output.Url, projectID, issueID));
            }

            return new V3.SendResult(V3.Result.Success,
                                   new Output(Output.Name,
                                              Output.Url,
                                              (rememberCredentials) ? false : Output.IntegratedAuthentication,
                                              (rememberCredentials) ? userName : Output.UserName,
                                              (rememberCredentials) ? password : Output.Password,
                                              Output.FileName,
                                              Output.FileFormat,
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
        return new V3.SendResult(V3.Result.Failed, ex.Message);
      }

    }

  }
}
