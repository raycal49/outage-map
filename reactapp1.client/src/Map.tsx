import { useState, useEffect, useRef, useCallback } from 'react';
import Map, { FullscreenControl, GeolocateControl, NavigationControl, Marker, Source, Layer, Popup, type MapMouseEvent, type MarkerDragEvent } from 'react-map-gl/mapbox';
import GeocoderControl from './Geocoder';
import './Map.css';

const MAPBOX_TOKEN = import.meta.env.VITE_MAPBOX_TOKEN;

function MyMap() {
    const [viewState, setViewState] = useState({
        longitude: -95.3698,
        latitude: 29.7604,
        zoom: 10.50
    });

    const [start, setStart] = useState([-95.3698, 29.7604]); // Downtown Houston
    const [end, setEnd] = useState([-95.6698, 29.9604]);
    const [coords, setCoords] = useState([]);
    //const [name, setName] = useState<string>();
    const [dist, setDist] = useState<number>();
    const [dur, setDur] = useState<number>();

    const geoRef = useRef<mapboxgl.GeolocateControl | null>(null);

    const getRoute = useCallback(async () => {
        const coordsParam = `${start[0]},${start[1]};${end[0]},${end[1]}`;
        const qs = new URLSearchParams({coordinates: coordsParam }).toString();
        const res = await fetch(`MetroRoute/Directions?${qs}`);
        const data = await res.json();
        const coords = data.routes[0].geometry.coordinates;
        //const origin = data.routes[0].waypoints[0].name;
        //const destination = data.routes[0].waypoints[data.routes[0].waypoints.length - 1].name;
        const distance = data.routes[0].distance;
        const duration = data.routes[0].duration;

        console.log(coords)
        setCoords(coords);
        //setName(origin + " to " + destination);
        setDist(distance);
        setDur(duration);

    }, [start, end]);

    useEffect(() => {
        getRoute()
    }, [end, start,getRoute])

    const geojson: GeoJSON.FeatureCollection<GeoJSON.LineString> = {
        type: "FeatureCollection",
        features: [
            {
                type: "Feature",
                properties: {},
                geometry: {
                    type: "LineString",
                    coordinates: coords
                }
            }
        ]
    };

    //type OutageFeature = GeoJSON.Feature<GeoJSON.Point, unknown>;

    type OutageProperties = {
        status?: string;
        numPeople?: number | string;
        etrTime?: number | string; // epoch millis, based on your code
    };

    type OutageFeature = GeoJSON.Feature<GeoJSON.Point, OutageProperties>;

    const [selectedOutage, setSelectedOutage] = useState<OutageFeature | null>(null);
    const [popupLngLat, setPopupLngLat] = useState<{ lng: number; lat: number } | null>(null);
    const [outageFc, setOutageFc] = useState<GeoJSON.FeatureCollection<GeoJSON.Point> | null>(null);

    useEffect(() => {
        (async () => {
            const res = await fetch("/OutageMap/OutageData");
            if (!res.ok) throw new Error(`OutageData failed (${res.status})`);
            const fc = await res.json();
            setOutageFc(fc);
        })().catch(console.error);
    }, []);

    const outageLayer: any = {
        id: "outage-points",
        type: "circle",
        paint: {
            // simple styling — tweak as you like
            "circle-radius": 6,
            "circle-opacity": 0.85,
            "circle-stroke-width": 1,
            "circle-stroke-color": "#000",

            // example: color by status if you have it in properties
            "circle-color": [
                "match",
                ["get", "status"],
                "Pending Assessment", "#f59e0b",
                "Crew Assessing", "#3b82f6",
                "Planned Outage", "#ef4444",
                "#10b981" // default
            ]
        }
    };


    const lineLayer = {
        id: 'roadLayer',
        type: 'line',
        source: {
            type: 'geojson',
            data: geojson
        },
        layout: {
            "line-join": 'round',
            "line-cap": 'round'
        },
        paint: {
            "line-color": "#3887be",
            "line-width": 5,
            "line-opacity": 0.75
        }
    } as const;

    const handleStartMarkerDragEnd = (e: MarkerDragEvent) => {
        const { lng, lat } = e.lngLat;
        setStart([lng, lat]);
    };

    const handleEndMarkerDragEnd = (e: MarkerDragEvent) => {
        const { lng, lat } = e.lngLat;
        setEnd([lng, lat]);
    };
     
    interface displayRouteDto {
        name: string,
        distance: number,
        duration: number
    };

    const handleClick = async (e: React.MouseEvent<HTMLButtonElement>) => {
        e.preventDefault();

        const input = window.prompt("Name this route:", name ?? "");
        if (input == null) return;              // user hit Cancel
        const routeName = input.trim();
        if (!routeName) return;                 // empty string

        if (!routeName || dist == null || dur == null) return;

        const payload: displayRouteDto = { name: routeName, distance: dist, duration: dur };

        try {
            const res = await fetch("MetroRoute/SaveRoute", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload),
                // credentials: "same-origin", // uncomment if your API uses cookies for auth
            });

            if (!res.ok) {
                throw new Error(`Save failed (${res.status})`);
            }

            if (res.ok) {
                console.log("saved successfully!");
            }

            // Your endpoint returns Ok() (empty 200). No JSON to parse.
        } catch (err) {
            console.error(err);
        }
    };

    const handleMapClick = (e: MapMouseEvent) => {
        // If interactiveLayerIds is set correctly, react-map-gl provides features here
        const f = e.features?.[0] as OutageFeature | undefined;
        if (!f) return;

        const [lng, lat] = (f.geometry as GeoJSON.Point).coordinates as [number, number];

        setSelectedOutage(f);
        setPopupLngLat({ lng, lat });
    };

    // todo: have the geocoder switch between either end marker or start marker as far as which one set the destination to.
    return (
        <>
            <div className="mapdiv">
                <Map
                    {...viewState}
                    onMove={(evt) => setViewState(evt.viewState)}
                    mapStyle="mapbox://styles/mapbox/streets-v12"
                    mapboxAccessToken={MAPBOX_TOKEN}
                    //style={{ width: "100vw", height: "100vh" }}
                    hash={true}
                    reuseMaps={true}
                    interactiveLayerIds={["outage-points"]}
                    onClick={handleMapClick}
                >
                    <GeocoderControl
                        mapboxAccessToken={MAPBOX_TOKEN}
                        position="top-left"
                        marker={false}
                    />

                    <Source id="routeSource" type="geojson" data={geojson}>
                        <Layer {...lineLayer} source="routeSource" />
                    </Source>

                    {outageFc && (
                        <Source id="outages" type="geojson" data={outageFc}>
                            <Layer {...outageLayer} />
                        </Source>
                    )}

                    {selectedOutage && popupLngLat && (
                        <Popup
                            longitude={popupLngLat.lng}
                            latitude={popupLngLat.lat}
                            anchor="top"
                            closeOnClick={false}
                            onClose={() => {
                                setSelectedOutage(null);
                                setPopupLngLat(null);
                            }}
                        >
                            {(() => {
                                const p = selectedOutage.properties ?? {};
                                const status = p.status ?? "—";
                                const people = p.numPeople != null ? Number(p.numPeople) : "—";
                                const etr =
                                    p.etrTime != null
                                        ? new Date(Number(p.etrTime)).toLocaleString()
                                        : "—";

                                return (
                                    <div style={{ minWidth: 200 }}>
                                        <div><b>Status:</b> {status}</div>
                                        <div><b>People:</b> {people}</div>
                                        <div><b>ETR:</b> {etr}</div>
                                    </div>
                                );
                            })()}
                        </Popup>
                    )}

                    {/*<Source id="endSource" type="geojson" data={endPoint}>*/}
                    {/*    <Layer {...layerEndpoint} source="endSource" />*/}
                    {/*</Source>*/}

                    <GeolocateControl ref={geoRef} />
                    <FullscreenControl />
                    <NavigationControl />
                    <Marker
                        longitude={start[0]}
                        latitude={start[1]}
                        draggable={true}
                        onDragEnd={handleStartMarkerDragEnd}
                    />
                    <Marker
                        longitude={end[0]}
                        latitude={end[1]}
                        draggable={true}
                        onDragEnd={handleEndMarkerDragEnd}
                    />
                </Map>

                <div className="save-overlay">
                    <button className="save-btn" type="button" onClick={handleClick}>Save</button>
                </div>
            </div>
        </>
    );
}

export default MyMap;
