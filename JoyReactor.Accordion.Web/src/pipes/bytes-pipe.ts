import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'bytes',
  standalone: true,
})
export class BytesPipe implements PipeTransform {
  private sizes = ['B', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];

  transform(bytes: number): string {
    if (!bytes)
      return '';

    if (bytes === 0)
      return '0 B';

    const sizeIndex = Math.floor(Math.log(bytes) / Math.log(1024));
    const size = parseFloat((bytes / Math.pow(1024, sizeIndex)).toFixed(2));

    return `${size} ${this.sizes[sizeIndex]}`;
  }
}