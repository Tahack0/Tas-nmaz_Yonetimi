import { AfterViewInit, ChangeDetectorRef, Component, DestroyRef, ElementRef, Input, OnDestroy, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { finalize, map } from 'rxjs';
import Feature from 'ol/Feature';
import WKT from 'ol/format/WKT';
import GeoJSON from 'ol/format/GeoJSON';
import Geometry from 'ol/geom/Geometry';
import OlMap from 'ol/Map';
import TileLayer from 'ol/layer/Tile';
import VectorLayer from 'ol/layer/Vector';
import { fromLonLat } from 'ol/proj';
import { isEmpty } from 'ol/extent';
import OSM from 'ol/source/OSM';
import VectorSource from 'ol/source/Vector';
import { Fill, Stroke, Style } from 'ol/style';
import View from 'ol/View';
import ScaleLine from 'ol/control/ScaleLine';
import { intersect } from '@turf/intersect';
import { featureCollection } from '@turf/helpers';
import type { Feature as GeoJsonFeature, Polygon, MultiPolygon } from 'geojson';
import { Property, PropertyResponse, PropertyType } from '../../core/property';

type MapFeature = Feature<Geometry>;
type PolygonFeature = GeoJsonFeature<Polygon | MultiPolygon>;

@Component({
  imports: [RouterLink],
  selector: 'app-property-map-overview',
  styleUrl: './property-map-overview.scss',
  templateUrl: './property-map-overview.html',
})
export class PropertyMapOverview implements AfterViewInit, OnDestroy {
  @Input() embedded = false;
  @ViewChild('mapElement') private readonly mapElement!: ElementRef<HTMLDivElement>;

  private readonly propertyService = inject(Property);
  private readonly http = inject(HttpClient);
  private readonly changeDetector = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly wktFormat = new WKT();
  private readonly geoJsonFormat = new GeoJSON();
  private readonly vectorSource = new VectorSource<MapFeature>();
  private readonly overlapSource = new VectorSource<MapFeature>();
  private readonly provinceSource = new VectorSource<MapFeature>();
  private readonly provinceLayer = new VectorLayer({
    source: this.provinceSource,
    style: new Style({
      fill: new Fill({ color: 'rgba(16, 185, 129, 0.08)' }),
      stroke: new Stroke({ color: '#047857', width: 2 }),
    }),
  });
  private readonly baseLayer = new TileLayer({ source: new OSM() });
  private readonly propertyLayer = new VectorLayer({
    source: this.vectorSource,
    style: new Style({
      fill: new Fill({ color: 'rgba(37, 99, 235, 0.25)' }),
      stroke: new Stroke({ color: '#2563eb', width: 3 }),
    }),
  });
  private readonly overlapLayer = new VectorLayer({
    source: this.overlapSource,
    visible: false,
    style: new Style({
      fill: new Fill({ color: 'rgba(249, 115, 22, 0.35)' }),
      stroke: new Stroke({ color: '#c2410c', width: 2 }),
    }),
  });

  // Tüm geometriler saklanır; harita kaynağı yalnız seçilenleri içerir.
  private readonly allFeatures = new Map<string, MapFeature>();
  private map?: OlMap;
  private calculationVersion = 0;
  private destroyed = false;
  protected properties: PropertyResponse[] = [];
  protected selectedIds = new Set<string>();
  protected selectedProperty: PropertyResponse | null = null;
  protected searchText = '';
  protected loading = true;
  protected calculating = false;
  protected errorMessage = '';
  protected overlapMessage = '';
  protected propertyCount = 0;
  protected overlapCount = 0;
  protected layerVisible = true;
  protected overlapVisible = false;
  protected baseOpacity = 100;
  protected layerOpacity = 100;
  protected overlapOpacity = 100;
  protected provinceVisible = true;
  protected provinceOpacity = 100;
  protected provinceLoading = true;
  protected provinceError = '';
  protected provinceCount = 0;

  // Arama yalnız listeyi daraltır; checkbox seçimlerini değiştirmez.
  protected get filteredProperties(): PropertyResponse[] {
    const search = this.searchText.trim().toLocaleLowerCase('tr');
    return this.properties.filter((p) =>
      `${p.city} ${p.district} ${p.neighborhood} ${p.lotNumber} ${p.parcelNumber}`
        .toLocaleLowerCase('tr').includes(search),
    );
  }

  ngAfterViewInit(): void {
    this.createMap();
    this.loadProvinceBoundaries();
    this.loadPropertyPage(1, []);
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.calculationVersion++;
    this.map?.dispose();
  }

  private createMap(): void {
    this.map = new OlMap({
      target: this.mapElement.nativeElement,
      layers: [this.baseLayer, this.provinceLayer, this.propertyLayer, this.overlapLayer],
      view: new View({ center: fromLonLat([35.2, 39]), zoom: 6 }),
    });
    this.map.addControl(new ScaleLine({
      units: 'metric', bar: true, steps: 4, text: true, minWidth: 120,
    }));
    this.map.on('singleclick', (event) => {
      // Turuncu katman taşınmaz detayına tıklamayı engellemez.
      const feature = this.map?.forEachFeatureAtPixel(
        event.pixel, (item) => item,
        { layerFilter: (layer) => layer === this.propertyLayer },
      );
      this.selectedProperty = feature?.get('property') ?? null;
      this.changeDetector.markForCheck();
    });
    this.map.on('pointermove', (event) => {
      if (event.dragging) return;
      const target = this.map?.getTargetElement();
      if (target) target.style.cursor = this.map?.hasFeatureAtPixel(
        event.pixel, { layerFilter: (layer) => layer === this.propertyLayer },
      ) ? 'pointer' : '';
    });
  }

  private loadProvinceBoundaries(): void {
    this.http.get<unknown>('data/turkiye-il-sinirlari.geojson').pipe(
      map((data) => this.readProvinceFeatures(data)),
      finalize(() => {
        this.provinceLoading = false;
        if (!this.destroyed) this.changeDetector.markForCheck();
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (features) => {
        this.provinceSource.addFeatures(features);
        this.provinceCount = features.length;
      },
      error: () => { this.provinceError = 'İl sınırları yüklenemedi. Sayfayı yenileyerek tekrar deneyin.'; },
    });
  }

  private readProvinceFeatures(data: unknown): MapFeature[] {
    const features = this.geoJsonFormat.readFeatures(data, {
      dataProjection: 'EPSG:4326', featureProjection: 'EPSG:3857',
    });
    if (!features.length || features.some((feature) => !this.isValidProvince(feature))) {
      throw new Error('İl sınırı verisi geçerli poligonlar içermiyor.');
    }
    return features;
  }

  private isValidProvince(feature: MapFeature): boolean {
    const geometry = feature.getGeometry();
    if (!geometry || !['Polygon', 'MultiPolygon'].includes(geometry.getType())) return false;
    const extent = geometry.getExtent();
    return !isEmpty(extent) && extent.every(Number.isFinite);
  }

  protected toggleProvinceLayer(event: Event): void {
    this.provinceVisible = (event.target as HTMLInputElement).checked;
    this.provinceLayer.setVisible(this.provinceVisible);
  }

  protected changeProvinceOpacity(event: Event): void {
    this.provinceOpacity = Number((event.target as HTMLInputElement).value);
    this.provinceLayer.setOpacity(this.provinceOpacity / 100);
  }

  private loadPropertyPage(page: number, collected: PropertyResponse[]): void {
    this.propertyService.list({ page, pageSize: 100 })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (response) => {
          const properties = [...collected, ...response.items];
          if (page < response.totalPages) {
            this.loadPropertyPage(page + 1, properties);
            return;
          }
          try {
            this.displayProperties(properties);
          } catch (error) {
            console.error('Harita geometri hatası:', error);
            this.errorMessage = 'Harita geometrileri işlenemedi.';
          } finally {
            this.loading = false;
            this.changeDetector.markForCheck();
          }
        },
        error: () => {
          this.loading = false;
          this.errorMessage = 'Taşınmazlar yüklenemedi.';
          this.changeDetector.markForCheck();
        },
      });
  }

  private displayProperties(properties: PropertyResponse[]): void {
    this.allFeatures.clear();
    this.properties = [];
    let invalidCount = 0;
    for (const property of properties) {
      try {
        const geometry = this.wktFormat.readGeometry(property.geometryWkt, {
          dataProjection: 'EPSG:4326', featureProjection: 'EPSG:3857',
        });
        const extent = geometry.getExtent();
        if (!['Polygon', 'MultiPolygon'].includes(geometry.getType()) ||
            isEmpty(extent) || !extent.every(Number.isFinite)) {
          throw new Error('Poligon geometrisi geçersiz.');
        }
        const feature = new Feature({ geometry });
        feature.setId(property.id);
        feature.set('property', property);
        this.allFeatures.set(property.id, feature);
        this.properties.push(property);
      } catch (error) {
        invalidCount++;
        console.error('Taşınmaz geometrisi okunamadı:', property.id, error);
      }
    }
    this.selectedIds = new Set(this.allFeatures.keys());
    this.refreshSelection();
    this.fitSelected();
    if (invalidCount) this.errorMessage = `${invalidCount} taşınmazın geometrisi okunamadı.`;
  }

  protected searchProperties(event: Event): void {
    this.searchText = (event.target as HTMLInputElement).value;
  }

  protected toggleProperty(id: string, event: Event): void {
    if ((event.target as HTMLInputElement).checked) this.selectedIds.add(id);
    else this.selectedIds.delete(id);
    this.refreshSelection();
  }

  protected selectAll(selected: boolean): void {
    this.selectedIds = selected ? new Set(this.allFeatures.keys()) : new Set();
    this.refreshSelection();
  }

  private refreshSelection(): void {
    this.vectorSource.clear();
    // Eksik olan satır: seçili poligonları gerçekten harita kaynağına ekler.
    this.vectorSource.addFeatures([...this.allFeatures.entries()]
      .filter(([id]) => this.selectedIds.has(id)).map(([, feature]) => feature));
    this.propertyCount = this.vectorSource.getFeatures().length;
    if (this.selectedProperty && !this.selectedIds.has(this.selectedProperty.id)) {
      this.selectedProperty = null;
    }
    this.updateOverlaps();
  }

  protected fitSelected(): void {
    const extent = this.vectorSource.getExtent();
    if (extent && !isEmpty(extent) && extent.every(Number.isFinite)) {
      this.map?.getView().fit(extent, {
        padding: [60, 60, 60, 60], maxZoom: 17, duration: 300,
      });
    }
  }

  protected togglePropertyLayer(event: Event): void {
    this.layerVisible = (event.target as HTMLInputElement).checked;
    this.propertyLayer.setVisible(this.layerVisible);
    if (!this.layerVisible) this.selectedProperty = null;
    // Kesişim katmanı kendi checkbox'ıyla bağımsız kontrol edilir.
  }

  protected toggleOverlaps(event: Event): void {
    this.overlapVisible = (event.target as HTMLInputElement).checked;
    this.updateOverlaps();
  }

  private updateOverlaps(): void {
    const version = ++this.calculationVersion;
    this.overlapSource.clear();
    this.overlapCount = 0;
    this.overlapMessage = '';
    this.calculating = false;
    this.overlapLayer.setVisible(this.overlapVisible);
    if (!this.overlapVisible || this.propertyCount < 2) return;
    this.calculating = true;
    void this.calculateOverlaps(version);
  }

  // Hesaplama aralarında ekranı serbest bırakır; eski seçim hesapları iptal edilir.
  private async calculateOverlaps(version: number): Promise<void> {
    const active = () => !this.destroyed && version === this.calculationVersion;
    await new Promise<void>((resolve) => setTimeout(resolve, 0));
    if (!active()) return;
    let failures = 0;
    try {
      const features = this.vectorSource.getFeatures();
      const indexes = new Map(features.map((feature, index) => [feature, index]));
      const polygons = new Map<MapFeature, PolygonFeature>();
      const readPolygon = (feature: MapFeature): PolygonFeature => {
        let polygon = polygons.get(feature);
        if (!polygon) {
          polygon = this.geoJsonFormat.writeFeatureObject(feature, {
            featureProjection: 'EPSG:3857', dataProjection: 'EPSG:4326',
          }) as PolygonFeature;
          polygons.set(feature, polygon);
        }
        return polygon;
      };
      const overlaps: MapFeature[] = [];
      let lastYield = performance.now();
      for (let i = 0; i < features.length; i++) {
        if (!active()) return;
        const geometry = features[i].getGeometry();
        if (!geometry) continue;
        // Mekânsal indeks sayesinde uzak taşınmaz çiftlerini karşılaştırmaz.
        const candidates = this.vectorSource.getFeaturesInExtent(geometry.getExtent());
        for (const second of candidates) {
          const j = indexes.get(second);
          if (j === undefined || j <= i) continue;
          try {
            const overlap = intersect(featureCollection([
              readPolygon(features[i]), readPolygon(second),
            ]));
            if (overlap) {
              const result = this.geoJsonFormat.readGeometry(overlap.geometry, {
                dataProjection: 'EPSG:4326', featureProjection: 'EPSG:3857',
              });
              if (result) overlaps.push(new Feature({ geometry: result }));
            }
          } catch (error) {
            failures++;
            console.error('Kesişim hesaplanamadı:', error);
          }
          if (performance.now() - lastYield > 12) {
            await new Promise<void>((resolve) => setTimeout(resolve, 0));
            if (!active()) return;
            lastYield = performance.now();
          }
        }
      }
      if (!active()) return;
      this.overlapSource.addFeatures(overlaps);
      this.overlapCount = overlaps.length;
      if (failures > 0) {
        this.overlapMessage = `${failures} çiftin kesişimi hesaplanamadı.`;
      } else if (overlaps.length > 0) {
        this.overlapMessage = '';
      } else {
        this.overlapMessage = 'Seçili taşınmazlarda ortak alan bulunamadı.';
      }
    } catch (error) {
      console.error('Kesişim hesaplama hatası:', error);
      if (active()) this.overlapMessage = 'Kesişimler hesaplanamadı.';
    } finally {
      if (active()) {
        this.calculating = false;
        this.changeDetector.markForCheck();
      }
    }
  }

  // Katmanların saydamlık değerleri birbirinden bağımsızdır.
  protected changeBaseOpacity(event: Event): void {
    this.baseOpacity = Number((event.target as HTMLInputElement).value);
    this.baseLayer.setOpacity(this.baseOpacity / 100);
  }
  protected changeLayerOpacity(event: Event): void {
    this.layerOpacity = Number((event.target as HTMLInputElement).value);
    this.propertyLayer.setOpacity(this.layerOpacity / 100);
  }
  protected changeOverlapOpacity(event: Event): void {
    this.overlapOpacity = Number((event.target as HTMLInputElement).value);
    this.overlapLayer.setOpacity(this.overlapOpacity / 100);
  }
  protected getPropertyTypeName(type: PropertyType): string {
    return ({ [PropertyType.Land]: 'Arsa', [PropertyType.Building]: 'Bina',
      [PropertyType.Residence]: 'Konut' })[type] ?? 'Bilinmiyor';
  }
  protected closeDetails(): void { this.selectedProperty = null; }
}
