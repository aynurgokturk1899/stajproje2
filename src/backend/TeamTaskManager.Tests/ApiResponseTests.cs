using TeamTaskManager.Api.Contracts;

namespace TeamTaskManager.Tests;

public sealed class ApiResponseTests
{
    [Fact]
    public void Ok_BasariliStandartCevapUretir()
    {
        var response = ApiResponse<string>.Ok("test", "İşlem başarılı.");

        Assert.True(response.Success);
        Assert.Equal("test", response.Data);
        Assert.Empty(response.Errors);
    }

    [Fact]
    public void Fail_HatalariStandartCevabaEkler()
    {
        var response = ApiResponse<object>.Fail("İstek doğrulanamadı.", "E-posta zorunludur.");

        Assert.False(response.Success);
        Assert.Null(response.Data);
        Assert.Contains("E-posta zorunludur.", response.Errors);
    }
}
