namespace tool_cantam.Core.Enums;

/// <summary>
/// Yüzde kodlamasının bağlamı. Aynı metin, kullanıldığı yere göre farklı kodlanmalıdır;
/// bu yüzden mod açıkça seçilir ve çözme tarafı da aynı modla yapılır.
/// </summary>
public enum UrlEncodingMode
{
    /// <summary>
    /// Tek bir bileşen (sorgu değeri, yol parçası). RFC 3986 "unreserved" kümesi korunur,
    /// diğer her şey yüzde kodlanır; boşluk <c>%20</c> olur.
    /// </summary>
    Component,

    /// <summary>
    /// <c>application/x-www-form-urlencoded</c> gövdesi: unreserved küme korunur,
    /// boşluk <c>+</c> olur. Bu yüzden gerçek bir artı işareti <c>%2B</c> olarak kodlanır.
    /// </summary>
    Form,

    /// <summary>
    /// Tam URL: şema ve ayırıcı karakterler (<c>:/?#[]@!$&amp;'()*+,;=</c>) korunur,
    /// yalnızca gerçekten geçersiz karakterler ve boşluk kodlanır.
    /// </summary>
    FullUrl
}
