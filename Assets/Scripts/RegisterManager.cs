using Newtonsoft.Json;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
public class RegisterManager<T> : MonoBehaviour
{
    [Header("URL 모음")]
    private string _baseURL = "https://localhost:7037/api/auth";
    private string _requestSendEmailCodeURL = "email-code/send";
    private string _verifyEmailCodeURL = "/email-code/verify";
    private string _registerURL = "/register";

    [Header("private 필드")]
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _nickName = string.Empty;
    private string _insertCode = string.Empty;

    [Header("Text 필드")]
    [SerializeField] private TMP_InputField emailText;
    [SerializeField] private TMP_InputField passwordText;
    [SerializeField] private TMP_InputField passwordCheckText;
    [SerializeField] private TMP_InputField nicNameText;
    [SerializeField] private TMP_InputField codeText;
    [SerializeField] private TextMeshProUGUI sendOrNot;//코드가 보내졌는지 확인하는 text

    [Header("DTO 필드")]
    [SerializeField] private RegisterResponse registerResponse;
    [SerializeField] private ApiResponse<T> apiResponse;

    public void OnRequestEmailCode()
    {
        if(emailText.text == string.Empty)
        {
            Debug.Log("이메일을 입력해주세요");
            return;
        }
        else
        {
            _email = emailText.text;
            StartCoroutine(RequestEmailCode());
        }
    }

    public void OnVerifyEmailCode()
    {
        StartCoroutine(VerifyEmailCode());
    }

    public IEnumerator RequestEmailCode()
    {   
        string url = _baseURL + _requestSendEmailCodeURL;
        var body = new SendVerificationCodeRequest
        {
            Email = _email
        };

        string json = JsonConvert.SerializeObject(body);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("이메일 인증 코드 전송 성공");
                sendOrNot.text = "인증코드가 전송되었습니다.";
            }
            else
            {
                Debug.LogError($"이메일 인증 코드 전송 실패: {request.error}");
                sendOrNot.text = "인증코드 전송 실패";
            }
        }
    }
    public IEnumerator VerifyEmailCode()
    {
        string url = _baseURL + _verifyEmailCodeURL;
        var body = new VerifyEmailCodeRequest
        {
            Email = _email,
            Code = codeText.text
        };
        
        string json = JsonConvert.SerializeObject(body);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("이메일 인증 코드 검증 성공");
                sendOrNot.text = "인증코드가 확인되었습니다.";
            }
            else
            {
                Debug.LogError($"이메일 인증 코드 검증 실패: {request.error}");
                sendOrNot.text = "인증코드 검증 실패";
            }
        }
    }
}
