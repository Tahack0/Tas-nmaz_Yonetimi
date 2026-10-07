import {AfterViewInit,Component,ElementRef,EventEmitter,Input,OnChanges,OnDestroy,Output,SimpleChanges,ViewChild,} from '@angular/core';
import Feature from 'ol/Feature';
import Map from 'ol/Map';
import View from 'ol/View';
import Draw from 'ol/interaction/Draw';
import TileLayer from 'ol/layer/Tile';
import VectorLayer from 'ol/layer/Vector';
import {Fill,Stroke,Style,} from 'ol/style';
import OSM from 'ol/source/OSM';
import VectorSource from 'ol/source/Vector';
import Polygon from 'ol/geom/Polygon';
import {fromLonLat,toLonLat,} from 'ol/proj';

@Component({
  selector: 'app-property-map',
  imports: [],
  templateUrl: './property-map.html',
  styleUrl: './property-map.scss',
})
export class PropertyMap
  implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('mapContainer')
  private mapContainer!: ElementRef<HTMLDivElement>;

  @Input()
  coordinates: number[][] = [];

  @Input()
  readonly = false;

  @Output()
  readonly coordinatesChange =
    new EventEmitter<number[][]>();

  protected drawing = false;
  protected pointCount = 0;

  private map?: Map;
  private draw?: Draw;

  private readonly vectorSource =
    new VectorSource();

  private readonly vectorLayer =
    new VectorLayer({
      source: this.vectorSource,
      style: new Style({
        stroke: new Stroke({
          color: '#2563eb',
          width: 3,
        }),
        fill: new Fill({
          color: '#2563eb33',
        }),
      }),
    });

  ngAfterViewInit(): void {
    this.map = new Map({
      target: this.mapContainer.nativeElement,
      layers: [
        new TileLayer({
          source: new OSM(),
        }),
        this.vectorLayer,
      ],
      view: new View({
        center: fromLonLat([
          35.2433,
          38.9637,
        ]),
        zoom: 6,
      }),
    });

    this.showCoordinates(this.coordinates);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (
      changes['coordinates'] &&
      this.map
    ) {
      this.showCoordinates(this.coordinates);
    }
  }

  ngOnDestroy(): void {
    this.removeDrawInteraction();
    this.map?.setTarget(undefined);
  }

  protected startDrawing(): void {
    if (!this.map || this.readonly) {
      return;
    }

    this.removeDrawInteraction();
    this.vectorSource.clear();

    this.coordinates = [];
    this.coordinatesChange.emit([]);
    this.pointCount = 0;
    this.drawing = true;

    this.draw = new Draw({
      source: this.vectorSource,
      type: 'Polygon',
      minPoints: 4,
      maxPoints: 4,
    });

    this.draw.on('drawstart', (event) => {
      const geometry =
        event.feature.getGeometry() as Polygon;

      geometry.on('change', () => {
        const ring =
          geometry.getCoordinates()[0] ?? [];

        // Sabit noktalar, hareketli imleç noktası ve
        // tekrar eden kapanış noktası birlikte gelir.
        this.pointCount = Math.min(
          Math.max(ring.length - 2, 0),
          4,
        );
      });
    });

    this.draw.on('drawend', (event) => {
      const geometry =
        event.feature.getGeometry() as Polygon;

      const ring =
        geometry.getCoordinates()[0];

      const selectedCoordinates = ring
        .slice(0, -1)
        .slice(0, 4)
        .map((coordinate) => {
          const [longitude, latitude] =
            toLonLat(coordinate);

          return [
            Number(longitude.toFixed(7)),
            Number(latitude.toFixed(7)),
          ];
        });

      this.coordinates = selectedCoordinates;
      this.pointCount = selectedCoordinates.length;
      this.drawing = false;

      this.coordinatesChange.emit(
        selectedCoordinates,
      );

      this.removeDrawInteraction();
    });

    this.map.addInteraction(this.draw);
  }

  protected clearDrawing(): void {
    if (this.readonly) {
      return;
    }

    this.removeDrawInteraction();
    this.vectorSource.clear();

    this.coordinates = [];
    this.pointCount = 0;
    this.drawing = false;

    this.coordinatesChange.emit([]);
  }

  private showCoordinates(
    coordinates: number[][],
  ): void {
    this.vectorSource.clear();

    if (
      !this.map ||
      coordinates.length < 4
    ) {
      return;
    }

    const fourPoints =
      coordinates.slice(0, 4);

    const projectedRing = [
      ...fourPoints.map((coordinate) =>
        fromLonLat([
          coordinate[0],
          coordinate[1],
        ]),
      ),
      fromLonLat([
        fourPoints[0][0],
        fourPoints[0][1],
      ]),
    ];

    const geometry = new Polygon([
      projectedRing,
    ]);

    this.vectorSource.addFeature(
      new Feature({
        geometry,
      }),
    );

    this.pointCount = 4;

    this.map.getView().fit(
      geometry.getExtent(),
      {
        padding: [50, 50, 50, 50],
        duration: 300,
        maxZoom: 17,
      },
    );
  }

  private removeDrawInteraction(): void {
    if (this.map && this.draw) {
      this.map.removeInteraction(this.draw);
      this.draw = undefined;
    }
  }
}