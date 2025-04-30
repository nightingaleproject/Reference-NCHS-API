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

    /// <summary>
    /// Converts a paylod version in the format XXXX_STU#_# to the format v#.#
    /// </summary>
    /// <param name="payload"></param>
    /// <returns></returns>
    public static string ConvertIGPaylodVersion(string payload) {
        string pattern = @"(?:BFDR|VRDR)_STU(\d+)_(\d+)";
        string replacement = @"$1.$2";
        string result = Regex.Replace(payload, pattern, replacement);
        Console.WriteLine(result);
        return "v" + result;
    }
}
