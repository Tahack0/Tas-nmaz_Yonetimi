import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  Input,
  OnDestroy,
  ViewChild,
  inject,
} from '@angular/core';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { finalize, map } from 'rxjs';
import GeoJSON from 'ol/format/GeoJSON';
import Geometry from 'ol/geom/Geometry';
import { isEmpty } from 'ol/extent';
import { RouterLink } from '@angular/router';
import Feature from 'ol/Feature';
import WKT from 'ol/format/WKT';
import OlMap from 'ol/Map';
import TileLayer from 'ol/layer/Tile';
import VectorLayer from 'ol/layer/Vector';
import { fromLonLat } from 'ol/proj';
import OSM from 'ol/source/OSM';
import VectorSource from 'ol/source/Vector';
import { Fill, Stroke, Style } from 'ol/style';
import View from 'ol/View';
import {PagedResponse,Property,PropertyResponse,PropertyType,} from '../../core/property';
import ScaleLine from 'ol/control/ScaleLine';

type MapFeature = Feature<Geometry>;

@Component({
  imports: [RouterLink],
  selector: 'app-property-map-overview',
  styleUrl: './property-map-overview.scss',
  templateUrl: './property-map-overview.html',
  
})
export class PropertyMapOverview
  implements AfterViewInit, OnDestroy
{
  @Input()
    embedded = false;

  // Haritanın yerleştirileceği HTML alanını yakalar.
  @ViewChild('mapElement')
  private mapElement!: ElementRef<HTMLDivElement>;

  // Backend taşınmaz servisidir.
  private readonly propertyService = inject(Property);
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly geoJsonFormat = new GeoJSON();
  private destroyed = false;
  private readonly provinceSource = new VectorSource<MapFeature>();
  private readonly provinceLayer = new VectorLayer({
    source: this.provinceSource,
    style: new Style({
      fill: new Fill({ color: 'rgba(16, 185, 129, 0.08)' }),
      stroke: new Stroke({ color: '#047857', width: 2 }),
    }),
  });
  protected provinceVisible = true;
  protected provinceOpacity = 100;
  protected provinceLoading = true;
  protected provinceError = '';
  protected provinceCount = 0;



  // Zoneless Angular yapısında ekranı günceller.
  private readonly changeDetector = inject(ChangeDetectorRef);

  // WKT metnini OpenLayers geometrisine dönüştürür.
  private readonly wktFormat = new WKT();

  // Taşınmaz poligonlarının tutulduğu katmandır.
  private readonly vectorSource = new VectorSource();

  // Taşınmazların gösterildiği vektör katmanıdır.
  private readonly propertyLayer = new VectorLayer({
    source: this.vectorSource,
    style: new Style({
      fill: new Fill({
        color: 'rgba(37, 99, 235, 0.25)',
      }),
      stroke: new Stroke({
        color: '#2563eb',
        width: 3,
      }),
    }),
  });

  // Taşınmaz katmanının görünürlük durumudur.
  protected layerVisible = true;

  // Taşınmaz katmanının yüzde cinsinden saydamlık değeridir.
  protected layerOpacity = 100;

  // OpenLayers harita nesnesidir.
  private map?: OlMap;

  // Haritada seçilen taşınmazın bilgisidir.
  protected selectedProperty: PropertyResponse | null = null;

  // Yükleme durumunu ekranda gösterir.
  protected loading = true;

  // Oluşabilecek servis veya geometri hatasını gösterir.
  protected errorMessage = '';

  // Haritada gösterilen toplam taşınmaz sayısıdır.
  protected propertyCount = 0;

  ngAfterViewInit(): void {
    this.createMap();
    this.loadProvinceBoundaries();
    this.loadPropertyPage(1, []);
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    // Sayfadan çıkıldığında haritayı DOM alanından ayırır.
    this.map?.setTarget(undefined);
  }

  private createMap(): void {
    // Türkiye merkezli harita oluşturur.
    this.map = new OlMap({
      target: this.mapElement.nativeElement,
      layers: [
        new TileLayer({
          source: new OSM(),
        }),
        this.provinceLayer,
        this.propertyLayer,
      ],
      view: new View({
        center: fromLonLat([35.2, 39]),
        zoom: 6,
      }),
    });

    this.map.addControl(
      new ScaleLine({
        units: 'metric',
        bar: true,
        steps: 4,
        text: true,
        minWidth: 120,
      }),
    );

    // Poligona tıklanınca taşınmaz bilgisini seçer.
    this.map.on('singleclick', (event) => {
      const feature = this.map?.forEachFeatureAtPixel(
        event.pixel,
        (clickedFeature) => clickedFeature,
      );

      this.selectedProperty =
        feature?.get('property') ?? null;

      this.changeDetector.detectChanges();
    });

    // Poligon üzerindeyken imleci el işaretine dönüştürür.
    this.map.on('pointermove', (event) => {
      const target =
        this.map?.getTargetElement();

      if (target) {
        target.style.cursor = this.map?.hasFeatureAtPixel(
          event.pixel,
        )
          ? 'pointer'
          : '';
      }
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

  private loadPropertyPage(
    page: number,
    collectedProperties: PropertyResponse[],
  ): void {
    // Backend en fazla 100 kayıt döndürdüğü için sayfaları sırayla çeker.
    this.propertyService
      .list({
        page,
        pageSize: 100,
      })
      .subscribe({
        next: (
          response: PagedResponse<PropertyResponse>,
        ) => {
          const allProperties = [
            ...collectedProperties,
            ...response.items,
          ];

          // Sonraki sayfa varsa onu da yükler.
          if (page < response.totalPages) {
            this.loadPropertyPage(page + 1, allProperties);
            return;
          }

          this.displayProperties(allProperties);
          this.loading = false;
          this.changeDetector.detectChanges();
        },
        error: () => {
          this.loading = false;
          this.errorMessage =
            'Taşınmazlar haritaya yüklenirken bir hata oluştu.';
          this.changeDetector.detectChanges();
        },
      });
  }

  private displayProperties(
    properties: PropertyResponse[],
  ): void {
    // Eski poligonların tekrar eklenmesini önler.
    this.vectorSource.clear();

    const features: Feature[] = [];

    for (const property of properties) {
      try {
        // Backend’den gelen EPSG:4326 WKT geometrisini
        // web haritasının EPSG:3857 sistemine dönüştürür.
        const feature = this.wktFormat.readFeature(
          property.geometryWkt,
          {
            dataProjection: 'EPSG:4326',
            featureProjection: 'EPSG:3857',
          },
        );

        // Tıklama sırasında erişmek için taşınmaz bilgisini saklar.
        feature.set('property', property);
        features.push(feature);
      } catch {
        // Hatalı tek bir geometri diğer kayıtları engellemez.
      }
    }

    this.vectorSource.addFeatures(features);
    this.propertyCount = features.length;

    // Poligon varsa haritayı tüm taşınmazları gösterecek şekilde yakınlaştırır.
    if (features.length > 0) {
      const extent = this.vectorSource.getExtent();

      if (extent !== null) {
        this.map?.getView().fit(extent, {
          padding: [60, 60, 60, 60],
          maxZoom: 17,
          duration: 500,
        });
      }
    }
    
  }

  protected getPropertyTypeName(
    propertyType: PropertyType,
  ): string {
    switch (propertyType) {
      case PropertyType.Land:
        return 'Arsa';

      case PropertyType.Building:
        return 'Bina';

      case PropertyType.Residence:
        return 'Konut';

      default:
        return 'Bilinmiyor';
    }
  }
  protected togglePropertyLayer(event: Event): void {
    // Checkbox durumuna göre taşınmaz katmanını gösterir veya gizler.
    const checkbox = event.target as HTMLInputElement;

    this.layerVisible = checkbox.checked;
    this.propertyLayer.setVisible(this.layerVisible);

    if (!this.layerVisible) {
      this.selectedProperty = null;
    }
  }

  protected changeLayerOpacity(event: Event): void {
    // Yüzde değerini OpenLayers'ın 0–1 saydamlık aralığına dönüştürür.
    const slider = event.target as HTMLInputElement;

    this.layerOpacity = Number(slider.value);
    this.propertyLayer.setOpacity(
      this.layerOpacity / 100,
    );
  }

  protected closeDetails(): void {
    // Seçilen taşınmaz bilgi panelini kapatır.
    this.selectedProperty = null;
  }
}