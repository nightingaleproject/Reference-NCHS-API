using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class AppSettings
{
    public Boolean AckAndIJEConversion { get; set; } = false;
    public Boolean BirthEnabled { get; set; } = false;
    public Boolean FetalDeathEnabled { get; set; } = false;
    public string SAMS {get; set;}
    public string STEVE {get; set;}
    public int PageCount {get; set;}
    public int MaxPayloadSize {get; set;}
    public List<string> SupportedBFDRIGVersions {get; set;}
    public List<string> SupportedVRDRIGVersions {get; set;}
    public string Environment {get; set;} = null;
}
