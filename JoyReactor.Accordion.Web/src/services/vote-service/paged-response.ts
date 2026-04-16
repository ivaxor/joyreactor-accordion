export interface PagedResponse<T> {
  values: T[],
  pages: number,
}