import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import Feature from 'ol/Feature';
import Geometry from 'ol/geom/Geometry';
import Polygon from 'ol/geom/Polygon';
import { vi } from 'vitest';
import { PropertyMapOverview } from './property-map-overview';

function square(x: number): Feature<Geometry> {
  return new Feature<Geometry>(new Polygon([
    [[x, 0], [x + 10, 0], [x + 10, 10], [x, 10], [x, 0]],
  ]));
}

describe('PropertyMapOverview overlap calculation', () => {
  let component: PropertyMapOverview;

  beforeEach(() => {
    vi.spyOn(PropertyMapOverview.prototype, 'ngAfterViewInit').mockImplementation(() => {});
    TestBed.configureTestingModule({
      imports: [PropertyMapOverview],
      providers: [provideHttpClient()],
    }).overrideComponent(PropertyMapOverview, { set: { template: '' } });
    component = TestBed.createComponent(PropertyMapOverview).componentInstance;
  });

  afterEach(() => vi.restoreAllMocks());

  it('calculates each intersecting pair once and excludes distant polygons', async () => {
    component['vectorSource'].addFeatures([square(0), square(5), square(100)]);

    const result = await component['collectOverlaps'](() => true);

    expect(result.failures).toBe(0);
    expect(result.overlaps).toHaveLength(1);
    expect((result.overlaps[0].getGeometry() as Polygon).getArea()).toBeCloseTo(50);
  });

  it('continues calculating other pairs after an intersection error', async () => {
    component['vectorSource'].addFeatures([square(0), square(2), square(4)]);
    vi.spyOn(component['geoJsonFormat'], 'writeFeatureObject')
      .mockImplementationOnce(() => { throw new Error('Invalid polygon'); });
    vi.spyOn(console, 'error').mockImplementation(() => {});

    const result = await component['collectOverlaps'](() => true);

    expect(result.failures).toBe(1);
    expect(result.overlaps).toHaveLength(2);
  });

  it('does not publish results when the selection changes during a calculation', async () => {
    component['vectorSource'].addFeatures([square(0), square(5)]);
    const publish = vi.spyOn(component['overlapSource'], 'addFeatures');
    const calculation = component['calculateOverlaps'](0);
    component['calculationVersion']++;

    await calculation;

    expect(publish).not.toHaveBeenCalled();
  });
});
