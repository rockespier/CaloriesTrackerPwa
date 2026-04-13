import { TestBed } from '@angular/core/testing';

import { NutritionServices } from './nutrition.services';

describe('NutritionServices', () => {
  let service: NutritionServices;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(NutritionServices);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
