using Newtonsoft.Json;
using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
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
    [SerializeField] private ApiResponse<RegisterResponse> registerApiResponse;
    [SerializeField] private ApiResponse<T> requestCodeResponse;
    [SerializeField] private ApiResponse<T> verifyCodeResponse;

    [SerializeField] private int _maxVerificationAttempts = 5;// 최대 인증 시도 횟수

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
            StartCoroutine(RequestEmailCode((response) =>
            {
                requestCodeResponse = response;

            }));
        }

    }

    public void OnVerifyEmailCode()
    {
        if(_maxVerificationAttempts > 0)
        StartCoroutine(VerifyEmailCode((response) =>
        {
            verifyCodeResponse = response;
        }));
    }

    public void OnRegister()
    {
        if (emailText.text == string.Empty)
        {
            Debug.Log("이메일을 입력해주세요");
            return;
        }

        if (passwordText.text == string.Empty)
        {
            Debug.Log("비밀번호를 입력해주세요");
            return;
        }

        if (passwordCheckText.text == string.Empty)
        {
            Debug.Log("비밀번호 확인을 입력해주세요");
            return;
        }

        if (passwordText.text != passwordCheckText.text)
        {
            Debug.Log("비밀번호가 일치하지 않습니다");
            return;
        }

        if (nicNameText.text == string.Empty)
        {
            Debug.Log("닉네임을 입력해주세요");
            return;
        }

        _email = emailText.text;
        _password = passwordText.text;
        _nickName = nicNameText.text;

        StartCoroutine(Register((response) =>
        {
            registerApiResponse = response;
            registerResponse = response.Data;
        }));
    }

    public IEnumerator RequestEmailCode(Action<ApiResponse<T>> response)
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
                response?.Invoke(JsonConvert.DeserializeObject<ApiResponse<T>>(request.downloadHandler.text));
                sendOrNot.text = "인증코드가 전송되었습니다.";
            }
            else
            {
                Debug.LogError($"이메일 인증 코드 전송 실패: {request.error}");
                sendOrNot.text = "인증코드 전송 실패";
            }
        }
    }
    public IEnumerator VerifyEmailCode(Action<ApiResponse<T>> response)
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
                response?.Invoke(JsonConvert.DeserializeObject<ApiResponse<T>>(request.downloadHandler.text));
                sendOrNot.text = "인증코드가 확인되었습니다.";
            }
            else
            {
                Debug.LogError($"이메일 인증 코드 검증 실패: {request.error}");
                sendOrNot.text = "인증코드 검증 실패";
                _maxVerificationAttempts--;
            }
        }
    }

    public IEnumerator Register(Action<ApiResponse<RegisterResponse>> response)
    {
        string url = _baseURL + _registerURL;
        var body = new RegisterRequest
        {
            Email = _email,
            Nickname = _nickName,
            Password = _password,
            ConfirmPassword = passwordCheckText.text
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
                Debug.Log("회원가입 성공");
                ApiResponse<RegisterResponse> apiResponse =
                    JsonConvert.DeserializeObject<ApiResponse<RegisterResponse>>(request.downloadHandler.text);

                if (apiResponse == null)
                {
                    Debug.LogError("회원가입 응답 파싱 실패");
                    sendOrNot.text = "회원가입 응답 오류";
                    yield break;
                }

                response?.Invoke(apiResponse);
                sendOrNot.text = apiResponse.Message;
            }
            else
            {
                Debug.LogError($"회원가입 실패: {request.error}");
                sendOrNot.text = "회원가입 실패";
            }
        }
    }
}
