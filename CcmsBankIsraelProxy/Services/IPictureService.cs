namespace CcmsBankIsraelProxy.Services;

/// <summary>
/// Service for updating employee pictures in BOI SAP (comda_pic_mi)
/// </summary>
public interface IPictureService
{
    Task<(bool success, string message)> UpdatePictureAsync(string idNum, string cardNum, string pictureBase64);
}
