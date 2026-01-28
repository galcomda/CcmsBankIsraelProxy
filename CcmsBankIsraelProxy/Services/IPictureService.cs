using CcmsBankIsraelProxy.Models;

namespace CcmsBankIsraelProxy.Services;

/// <summary>
/// Service for updating employee pictures in BOI SAP (comda_pic_mi)
/// </summary>
public interface IPictureService
{
    /// <summary>
    /// Updates employee picture in SAP
    /// </summary>
    Task<(bool success, string message)> UpdatePictureAsync(PictureUpdateRequest request);
}
