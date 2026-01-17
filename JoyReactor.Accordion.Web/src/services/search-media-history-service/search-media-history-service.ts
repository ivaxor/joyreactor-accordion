import { Injectable } from '@angular/core';
import { BehaviorSubject, from, map, Observable, switchMap, tap } from 'rxjs';
import { SearchMediaHistoryRecord } from './search-media-history-record';
import Dexie, { Table } from 'dexie';
import { SearchResponse } from '../search-service/search-response';

@Injectable({
  providedIn: 'root',
})
export class SearchMediaHistoryService extends Dexie {
  private searchMediaHistory: Table<SearchMediaHistoryRecord, number>;
  private recordsSubject = new BehaviorSubject<SearchMediaHistoryRecord[]>([]);
  private offset = 0;
  private limit = 20;

  constructor() {
    super('JoyReactorAccordion');
    this.version(1).stores({ searchMediaHistory: '++id, createdAt' });
    this.searchMediaHistory = this.table('searchMediaHistory');

    this.list().then(historyRecords => this.recordsSubject.next(historyRecords));
  }

  records$ = this.recordsSubject.asObservable();
  get records(): SearchMediaHistoryRecord[] {
    return this.recordsSubject.getValue();
  }

  private list(): Promise<SearchMediaHistoryRecord[]> {
    return this.searchMediaHistory
      .orderBy('createdAt')
      .reverse()
      .offset(this.offset)
      .limit(this.limit)
      .toArray();
  }

  async addDownload(mediaUrl: string, results: SearchResponse[]): Promise<number> {
    const record: SearchMediaHistoryRecord = {
      url: mediaUrl,
      results: results,
      createdAt: new Date(),
    };
    record.id = await this.searchMediaHistory.add(record);

    const newRecords = [record, ...this.records.filter((_, i) => i < this.limit - 1)];
    this.recordsSubject.next(newRecords);

    return record.id;
  }

  async addUpload(file: File, results: SearchResponse[]): Promise<number> {
    const record: SearchMediaHistoryRecord = {
      fileName: file.name,
      results: results,
      createdAt: new Date(),
    };
    record.id = await this.searchMediaHistory.add(record);

    const newRecords = [record, ...this.records.filter((_, i) => i < this.limit - 1)];
    this.recordsSubject.next(newRecords);

    return record.id;
  }

  async clear(): Promise<void> {
    await this.searchMediaHistory.clear();
    this.recordsSubject.next([]);
  }
}