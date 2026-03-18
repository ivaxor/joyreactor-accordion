import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CrawlerTaskResponse } from '../../../services/crawler-task-service/crawler-task-response';

@Component({
  selector: 'app-crawler-task-info',
  imports: [],
  templateUrl: './crawler-task-info.html',
  styleUrl: './crawler-task-info.scss',
})
export class CrawlerTaskInfo implements OnChanges {
  @Input({ required: true }) crawlerTask!: CrawlerTaskResponse;
  url: string | null = null;
  percents: number | null = null;
  isActive: boolean | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.crawlerTask)
      return;

    this.url = this.crawlerTask.tag.api ?
      `https://${this.crawlerTask.tag.api.hostName}/tag/${this.crawlerTask.tag.name}`
      : null;

    this.percents = this.crawlerTask.pageLast && this.crawlerTask.pageCurrent
      ? 100.0 / (this.crawlerTask.pageLast ?? 1.0) * (this.crawlerTask.pageCurrent ?? 0.0)
      : null;

    const date = Date.now();
    this.crawlerTask.updatedAt = new Date(this.crawlerTask.updatedAt);
    const difference = date - this.crawlerTask.updatedAt.getTime();
    this.isActive = difference <= 3600000;
  }
}