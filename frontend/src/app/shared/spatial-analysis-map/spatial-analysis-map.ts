import {
  AfterViewInit, Component, ElementRef, EventEmitter,
  OnDestroy, Output, ViewChild,
} from '@angular/core';
import Feature from 'ol/Feature';
import Map from 'ol/Map';
import View from 'ol/View';
import WKT from 'ol/format/WKT';
import Polygon from 'ol/geom/Polygon';
import Draw from 'ol/interaction/Draw';
import TileLayer from 'ol/layer/Tile';
import VectorLayer from 'ol/layer/Vector';
import OSM from 'ol/source/OSM';
import VectorSource from 'ol/source/Vector';
import { fromLonLat } from 'ol/proj';
import { Fill, Stroke, Style, Text } from 'ol/style';
import {
  SaveSpatialGeometriesRequest,
  SpatialAnalysisResponse,
} from '../../core/spatial-analysis';

@Component({
  selector: 'app-spatial-analysis-map',
  templateUrl: './spatial-analysis-map.html',
  styleUrl: './spatial-analysis-map.scss',
})
export class SpatialAnalysisMap implements AfterViewInit, OnDestroy {
  @ViewChild('mapElement')
  private readonly mapElement!: ElementRef<HTMLDivElement>;

  @Output()
  readonly geometriesChange =
    new EventEmitter<SaveSpatialGeometriesRequest | null>();

  private readonly source = new VectorSource();
  private readonly resultSource = new VectorSource();
  private readonly wkt = new WKT();
  private readonly rings: number[][][] = [];
  private map?: Map;
  private draw?: Draw;

  ngAfterViewInit(): void {
    this.map = new Map({
      target: this.mapElement.nativeElement,
      layers: [
        new TileLayer({ source: new OSM() }),
        new VectorLayer({
          source: this.source,
          style: (feature) => this.createStyle(feature.get('label'), '#2563eb'),
        }),
        new VectorLayer({
          source: this.resultSource,
          style: this.createStyle('Sonuç', '#e11d48'),
        }),
      ],
      view: new View({ center: fromLonLat([35.2, 39]), zoom: 6 }),
    });
  }

  // Manuel çizim için haritayı temizleyip çizimi başlatır.
  startDrawing(): void {
    this.clear();
    this.draw = new Draw({ source: this.source, type: 'Polygon' });

    this.draw.on('drawend', (event) => {
      const geometry = event.feature.getGeometry();
      if (!(geometry instanceof Polygon)) return;

      const ring = geometry.clone()
        .transform('EPSG:3857', 'EPSG:4326').getCoordinates()[0];

      event.feature.set('label', ['A', 'B', 'C'][this.rings.length]);
      this.rings.push(ring);

      if (this.rings.length === 3) {
        this.draw?.setActive(false);
        this.geometriesChange.emit({
          a: this.rings[0], b: this.rings[1], c: this.rings[2],
        });
      }
    });

    this.map?.addInteraction(this.draw);
  }

  // Auto-Select ile getirilen A/B/C geometrilerini gösterir.
  showSaved(analyses: SpatialAnalysisResponse[]): void {
    this.clear();
    const features = analyses.map((analysis) => {
      const feature = this.readFeature(analysis.geometryWkt);
      feature.set('label', ['A', 'B', 'C'][analysis.label - 1]);
      return feature;
    });

    this.source.addFeatures(features);
    const extent = this.source.getExtent();
    if (features.length && extent) {
      this.map?.getView().fit(extent, {
        padding: [50, 50, 50, 50], maxZoom: 17,
      });
    }
  }

  // Hesaplanan kesişim veya birleşimi ayrı katmanda gösterir.
  showResult(result: SpatialAnalysisResponse): void {
    this.clearResult();
    this.resultSource.addFeature(this.readFeature(result.geometryWkt));
  }

  clearResult(): void {
    this.resultSource.clear();
  }

  // Yalnızca ekrandaki geometrileri temizler.
  clear(): void {
    if (this.draw) this.map?.removeInteraction(this.draw);
    this.draw = undefined;
    this.source.clear();
    this.clearResult();
    this.rings.length = 0;
    this.geometriesChange.emit(null);
    this.map?.getView().setCenter(fromLonLat([35.2, 39]));
    this.map?.getView().setZoom(6);
  }

  ngOnDestroy(): void {
    this.map?.dispose();
  }

  private readFeature(wkt: string): Feature {
    const feature = this.wkt.readFeature(wkt, {
      dataProjection: 'EPSG:4326',
      featureProjection: 'EPSG:3857',
    });
    if (!feature) {
      throw new Error('WKT does not contain a geometry.');
    }
    return feature;
  }

  private createStyle(label: string, color: string): Style {
    return new Style({
      stroke: new Stroke({ color, width: 3 }),
      fill: new Fill({ color: `${color}33` }),
      text: new Text({
        text: label,
        font: 'bold 16px sans-serif',
        fill: new Fill({ color }),
        stroke: new Stroke({ color: '#fff', width: 3 }),
      }),
    });
  }
}
