namespace CcmsBankIsraelProxy.Models;

/// <summary>
/// Request to update employee picture in BOI SAP (poli_pic_req_dt)
/// </summary>
public class PictureUpdateRequest
{
    public string IdNum { get; set; } = string.Empty;
    public string CardNum { get; set; } = string.Empty;
    
    /// <summary>
    /// Base64 encoded JPEG image
    /// </summary>
    public string? Picture { get; set; }
}

/// <summary>
/// Response from BOI SAP picture update (poli_pic_res_dt)
/// </summary>
public class PictureUpdateResponse
{
    public bool Success { get; set; }
    public List<PictureUpdateMessage> Messages { get; set; } = [];
}

public class PictureUpdateMessage
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
}
