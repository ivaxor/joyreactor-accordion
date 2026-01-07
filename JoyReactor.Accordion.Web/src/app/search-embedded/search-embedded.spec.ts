import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SearchEmbedded } from './search-embedded';

describe('SearchEmbedded', () => {
  let component: SearchEmbedded;
  let fixture: ComponentFixture<SearchEmbedded>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SearchEmbedded]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SearchEmbedded);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
