import { AxiosError } from 'axios';


/**
 * handleRequest.ts
 * -------------------------------------------------
 * Một wrapper chung cho tất cả các lời gọi API.
 * - Ghi log lỗi (URL + status) → luôn có trong Console.
 * - Không thực hiện redirect (`window.location.replace`) khi lỗi không phải 401.
 * - Trả về một tuple `[data, error]` để component có thể quyết định UI.
 * - Có thể truyền `fallback` để UI hiển thị dữ liệu dự phòng.
 * -------------------------------------------------
 */

export interface HandleRequestOptions<T> {
  /** Giá trị trả về khi API lỗi (để UI có thể render một fallback) */
  fallback?: T;
  /** Hàm hiển thị thông báo người dùng (ví dụ: toast) */
  onError?: (err: Error | AxiosError) => void;
}

/**
 * Wrapper chung.
 *
 * @param request   Promise trả về dữ liệu từ API (có thể là `apiClient.get`, `apiClient.post`, …)
 * @param options   Các tùy chọn (fallback, onError,…)
 *
 * @returns tuple `[data, error]`
 *   - `data` là giá trị trả về nếu thành công, hoặc `fallback` nếu có.
 *   - `error` là `null` khi thành công, hoặc đối tượng lỗi khi thất bại.
 */
export async function handleRequest<T>(
  request: Promise<T>,
  options?: HandleRequestOptions<T>
): Promise<[T | undefined, Error | AxiosError | null]> {
  try {
    const data = await request;
    return [data, null];
  } catch (e) {
    const err = e as Error | AxiosError;
    const url = (err as AxiosError).config?.url ?? '(unknown URL)';
    const status = (err as AxiosError).response?.status;
    const message = (err as AxiosError).message;

    if (status) {
      console.warn(`[API] ${status} on ${url}`, (err as AxiosError).response?.data);
    } else {
      console.warn(`[API] Network error on ${url}:`, message);
    }

    if (options?.onError) {
      options.onError(err);
    }

    return [options?.fallback, err];
  }
}
