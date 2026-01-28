using BOI;
using BOI.Models;
using System;
using Microsoft.Rest;
using System.Net.Http;
using WebApp.SMS;
using WebApp.SAP;

public class BOI_WebServices
{
    public void FindUser()
    {
        SapComda sapComda = new SapComda(new BasicAuthenticationCredentials() { UserName = "sap_usr", Password = "sap_pwd" });
        sapComda.BaseUri = new Uri("https://sap_uri/...");
        ComdaResMt res = sapComda.Comda.Get("UserID");

        if (res != null)
        {
            EmpData data = res.EmpData;
        }
    }

    public bool UpdateSAP()
    {
        comda_pic_miClient client = new comda_pic_miClient("HTTP_Port");
        client.ClientCredentials.UserName.UserName = "sap_usr";
        client.ClientCredentials.UserName.Password = "sap_pwd";
        byte[] jpeg = ...;

        poli_pic_req_dt request = new poli_pic_req_dt
        {
            IDNum = "ID123",
            CardNum = "CardID",
            Picture = jpeg == null ? null : Convert.ToBase64String(jpeg)
        };

        poli_pic_res_dtReturnMessage[] ret = client.poli_pic_mi(request);

        if (ret == null || ret.Length == 0)
            return false; // Error

        return true;
    }

    public void UpdateSRHR()
    {
        string uri = "https://srhr_uri/...";
        ComdaService cs = new ComdaService(new BasicAuthenticationCredentials(), new HttpClientHandler { UseDefaultCredentials = true });
        cs.BaseUri = new Uri(uri);
        byte[] jpeg = ...;

        Polimil polimil = new Polimil()
        {
            CardId = "CardID",
            EmployeeId = "EmployeeID",
            EmployeeType = "employeeType",
            Factory = "0000",
            FirstName = "FirstName",
            LastName = "LastName",
            Id = "ID123",
            Image = jpeg
        };
        
        cs.PolimilOperations.Post(polimil);
    }

    public void SendSms(string phone, string message)
    {
        Service1 sms = new Service1();
        string response = sms.SendSMS(phone, message);
    }
}