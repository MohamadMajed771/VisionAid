import { API_CONFIG } from "./config";
import { fetch as expoFetch } from "expo/fetch";
const BASE_URL = API_CONFIG.baseUrl;

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string
  ) {
    super(message);
    this.name = "ApiError";
  }
}

export async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {},
  token?: string,
  timeoutMs: number = 15000
): Promise<T> {
  if (!BASE_URL) {
    throw new Error("EXPO_PUBLIC_API_URL is missing.");
  }

  const headers = new Headers(options.headers);

  headers.set("Accept", "application/json");

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  if (
    options.body &&
    !(options.body instanceof FormData)
  ) {
    headers.set("Content-Type", "application/json");
  }
  

  const controller = new AbortController();

  const timeoutId = setTimeout(() => {
    controller.abort();
  }, timeoutMs);

  let response: Response;
  let text: string;
  console.log("API URL:", BASE_URL);
  console.log("Request endpoint:", endpoint);
  try {
  
    const url = `${BASE_URL}${endpoint}`;

    const requestOptions = {
      ...options,
      headers,
      signal: controller.signal,
    };

    if (options.body instanceof FormData) {
      // Camera image upload.
      response = await expoFetch(url, requestOptions);
    } else {
      // Login, Register, Sessions, and normal JSON requests.
      response = await fetch(url, requestOptions);
    }


    text = await response.text();
  }catch (error) {
  if (__DEV__) {
  console.log(
    "API request failed:",
    endpoint,
    error instanceof Error ? error.message : String(error)
  );
}

  if (controller.signal.aborted) {
    throw new Error("Request timed out.");
  }

  throw new Error("Cannot connect to VisionAid API.");
}finally {
    clearTimeout(timeoutId);
  }


  let data: any = null;

  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = text;
    }
  }

  if (!response.ok) {
    const message =
      typeof data === "string"
        ? data
        : data?.message ??
          data?.title ??
          `Request failed (${response.status})`;

    throw new ApiError(response.status, message);
  }

  return data as T;
}
