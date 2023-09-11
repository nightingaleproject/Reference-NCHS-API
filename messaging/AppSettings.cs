using System;

public class AppSettings
{
    public Boolean AckAndIJEConversion { get; set; } = false;
    public string SAMS {get; set;}
    public string STEVE {get; set;}
    public int PageCount {get; set;}
    public string ClientId {get; set;}
    public string ClientSecret {get; set;}
    public string AuthEndpoint {get; set;}
    public string TokenEndpoint {get; set;}
    public string UserInfo {get; set;}
    public string CallbackPath {get; set;}
    
}
