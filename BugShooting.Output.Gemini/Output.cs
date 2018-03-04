using BS.Plugin.V3.Output;
using System;

namespace BugShooting.Output.Gemini
{

  public class Output: IOutput 
  {
    
    string name;
    string url;
    string userName;
    string password;
    bool integratedAuthentication;
    string fileName;
    Guid fileFormatID;
    bool openItemInBrowser;
    int lastProjectID;
    int lastIssueTypeID;
    int lastIssueID;

    public Output(string name, 
                  string url,
                  bool integratedAuthentication,
                  string userName,
                  string password,
                  string fileName,
                  Guid fileFormatID,
                  bool openItemInBrowser, 
                  int lastProjectID, 
                  int lastIssueTypeID,
                  int lastIssueID)
    {
      this.name = name;
      this.url = url;
      this.integratedAuthentication = integratedAuthentication;
      this.userName = userName;
      this.password = password;
      this.fileName = fileName;
      this.fileFormatID = fileFormatID;
      this.openItemInBrowser = openItemInBrowser;
      this.lastProjectID = lastProjectID;
      this.lastIssueTypeID = lastIssueTypeID;
      this.lastIssueID = lastIssueID;
    }
    
    public string Name
    {
      get { return name; }
    }

    public string Information
    {
      get { return url; }
    }

    public string Url
    {
      get { return url; }
    }

    public bool IntegratedAuthentication
    {
      get { return integratedAuthentication; }
    }

    public string UserName
    {
      get { return userName; }
    }

    public string Password
    {
      get { return password; }
    }

    public string FileName
    {
      get { return fileName; }
    }

    public Guid FileFormatID
    {
      get { return fileFormatID; }
    }

    public bool OpenItemInBrowser
    {
      get { return openItemInBrowser; }
    }
    
    public int LastProjectID
    {
      get { return lastProjectID; }
    }

    public int LastIssueTypeID
    {
      get { return lastIssueTypeID; }
    }

    public int LastIssueID
    {
      get { return lastIssueID; }
    }

  }
}
