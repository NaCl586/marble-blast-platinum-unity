using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Server.Config;
using Server.DTOs.Responses;
using Server.Exceptions;

namespace Server.API
{
    public class ApiClient
    {
        private readonly ServerConfig _config;

        public string? Token
        {
            get;
            private set;
        }

        public bool HasToken => !string.IsNullOrWhiteSpace(Token);

        public ApiClient(ServerConfig config)
        {
            _config = config;
        }

        public void SetToken(string token)
        {
            Token = token;
        }

        public void ClearToken()
        {
            Token = null;
        }

        private string BuildUrl(string route)
        {
            route = route.TrimStart('/');

            return
                $"{_config.BaseUrl.TrimEnd('/')}/{route}";
        }

        private void ApplyHeaders(UnityWebRequest request)
        {
            request.timeout = _config.Timeout;

            if (!string.IsNullOrWhiteSpace(Token))
            {
                request.SetRequestHeader(
                    "Authorization",
                    $"Bearer {Token}");
            }
        }

        private async UniTask SendAsync(UnityWebRequest request)
        {
            ApplyHeaders(request);

            if (_config.LogRequests)
            {
                Debug.Log(
                    $"--> {request.method} {request.url}");
            }

            try
            {
                await request.SendWebRequest();
            }
            catch (UnityWebRequestException)
            {
                // UniTask melempar exception untuk HTTP error.
                // Abaikan dulu supaya kita bisa memetakan sendiri.
            }

            if (_config.LogRequests)
            {
                Debug.Log(
                    $"<-- {(int)request.responseCode}");

                if (request.downloadHandler != null)
                {
                    Debug.Log(request.downloadHandler.text);
                }
            }

            switch (request.responseCode)
            {
                case 200:
                case 201:
                case 204:
                    return;

                case 400:
                case 401:
                case 403:
                case 404:
                case 409:
                    ThrowHttpException(request);
                    return;

                case 0:
                    throw new NetworkException(
                        request.error ?? "Network error.");

                default:
                    throw new ApiException(
                        (int)request.responseCode,
                        ExtractErrorMessage(request));
            }
        }

        private TResponse ReadResponse<TResponse>(
            DownloadHandler handler)
        {
            ApiResponse<TResponse>? response =
                JsonConvert.DeserializeObject<ApiResponse<TResponse>>(
                    handler.text);

            if (response == null)
            {
                throw new ApiException(
                    500,
                    "Failed to deserialize server response.");
            }

            if (response.Data == null)
            {
                throw new ApiException(
                    500,
                    "Server returned empty response.");
            }

            return response.Data;
        }

        public async UniTask<TResponse> GetAsync<TResponse>(
            string route)
        {
            using UnityWebRequest request =
                UnityWebRequest.Get(
                    BuildUrl(route));

            request.downloadHandler =
                new DownloadHandlerBuffer();

            await SendAsync(request);

            return ReadResponse<TResponse>(
                request.downloadHandler);
        }

        public UniTask<TResponse> PostJsonAsync<TRequest, TResponse>(
            string route,
            TRequest body)
        {
            return SendJsonAsync<TRequest, TResponse>(
                UnityWebRequest.kHttpVerbPOST,
                route,
                body);
        }

        public async UniTask<TResponse> UploadFileAsync<TResponse>(
            string route,
            string formFieldName,
            string filePath)
        {
            byte[] bytes =
                File.ReadAllBytes(filePath);

            WWWForm form =
                new WWWForm();

            form.AddBinaryData(
                formFieldName,
                bytes,
                Path.GetFileName(filePath));

            using UnityWebRequest request =
                UnityWebRequest.Post(
                    BuildUrl(route),
                    form);

            await SendAsync(request);

            return ReadResponse<TResponse>(
                request.downloadHandler);
        }

        public async UniTask DownloadFileAsync(
            string route,
            string savePath)
        {
            using UnityWebRequest request =
                UnityWebRequest.Get(
                    BuildUrl(route));

            request.downloadHandler =
                new DownloadHandlerFile(
                    savePath);

            await SendAsync(request);
        }

        private async UniTask<TResponse> SendJsonAsync<TRequest, TResponse>(
            string method,
            string route,
            TRequest body)
        {
            string json =
                JsonConvert.SerializeObject(body);

            using UnityWebRequest request =
                new UnityWebRequest(
                    BuildUrl(route),
                    method);

            request.uploadHandler =
                new UploadHandlerRaw(
                    Encoding.UTF8.GetBytes(json));

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type",
                "application/json");

            await SendAsync(request);

            return ReadResponse<TResponse>(
                request.downloadHandler);
        }

        public UniTask<TResponse> PutJsonAsync<TRequest, TResponse>(
            string route,
            TRequest body)
        {
            return SendJsonAsync<TRequest, TResponse>(
                UnityWebRequest.kHttpVerbPUT,
                route,
                body);
        }

        public async UniTask<TResponse> DeleteAsync<TResponse>(
            string route)
        {
            using UnityWebRequest request =
                UnityWebRequest.Delete(
                    BuildUrl(route));

            request.downloadHandler =
                new DownloadHandlerBuffer();

            await SendAsync(request);

            return ReadResponse<TResponse>(
                request.downloadHandler);
        }

        private static string ExtractErrorMessage(
            UnityWebRequest request)
        {
            return request.downloadHandler?.text
                   ?? request.error
                   ?? "Unknown server error.";
        }

        private static void ThrowHttpException(
            UnityWebRequest request)
        {
            string message =
                ExtractErrorMessage(request);

            switch (request.responseCode)
            {
                case 400:
                    throw new ValidationException(message);

                case 401:
                    throw new UnauthorizedException(message);

                case 403:
                    throw new ForbiddenException(message);

                case 404:
                    throw new NotFoundException(message);

                case 409:
                    throw new ConflictException(message);

                default:
                    throw new ApiException(
                        (int)request.responseCode,
                        message);
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Token);
            }
        }
    }
}