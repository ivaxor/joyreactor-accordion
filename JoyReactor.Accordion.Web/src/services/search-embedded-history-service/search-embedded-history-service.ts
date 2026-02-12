import { Injectable } from '@angular/core';
import Dexie, { Table } from 'dexie';
import { SearchEmbeddedHistoryRecord } from './search-embedded-history-record';
import { BehaviorSubject } from 'rxjs';
import { SearchEmbeddedResponse } from '../search-embedded-service/search-embedded-response';
import { SearchEmbeddedRequest } from '../search-embedded-service/search-embedded-request';

@Injectable({
  providedIn: 'root',
})
export class SearchEmbeddedHistoryService extends Dexie {
  private searchEmbeddedHistory: Table<SearchEmbeddedHistoryRecord, number>;
  private recordsSubject = new BehaviorSubject<SearchEmbeddedHistoryRecord[]>([]);
  private offset = 0;
  private limit = 20;

  constructor() {
    super('JoyReactorAccordion');
    this.version(1).stores({ searchMediaHistory: '++id, createdAt' });
    this.searchEmbeddedHistory = this.table('searchEmbeddedHistory');

    this.list().then(historyRecords => this.recordsSubject.next(historyRecords));
  }

  records$ = this.recordsSubject.asObservable();
  get records(): SearchEmbeddedHistoryRecord[] {
    return this.recordsSubject.getValue();
  }

  private list(): Promise<SearchEmbeddedHistoryRecord[]> {
    return this.searchEmbeddedHistory
      .orderBy('createdAt')
      .reverse()
      .offset(this.offset)
      .limit(this.limit)
      .toArray();
  }

  async add(request: SearchEmbeddedRequest, response: SearchEmbeddedResponse): Promise<number> {
    const record: SearchEmbeddedHistoryRecord = {
      type: request.type,
      text: request.text,
      postIds: response.postIds,
      createdAt: new Date(),
    };
    record.id = await this.searchEmbeddedHistory.add(record);

    const newRecords = [record, ...this.records.filter((_, i) => i < this.limit - 1)];
    this.recordsSubject.next(newRecords);

    return record.id;
  }

  async clear(): Promise<void> {
    await this.searchEmbeddedHistory.clear();
    this.recordsSubject.next([]);
  }
}