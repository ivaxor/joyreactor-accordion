import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SearchPicture } from './search-picture';

describe('SearchPicture', () => {
  let component: SearchPicture;
  let fixture: ComponentFixture<SearchPicture>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SearchPicture]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SearchPicture);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
