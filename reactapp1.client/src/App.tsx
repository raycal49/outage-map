import MyMap from './Map.tsx'
import 'mapbox-gl/dist/mapbox-gl.css'
import './App.css'
import { BrowserRouter, Routes, Route, Link, NavLink } from 'react-router';
import Dashboard from './Dashboard.tsx';

function App() {
    return (
        <BrowserRouter>

            <Routes>
                <Route path="/" element={< MyMap /> } />
                <Route path="/dashboard" element={<Dashboard /> } />
            </Routes>

            <nav className="map-links mapboxgl-ctrl mapboxgl-ctrl-group">
                <NavLink to="/dashboard">Dashboard View</NavLink>
                <NavLink to="/">Map View</NavLink>
            </nav>
        </BrowserRouter>
    );
}

export default App;

//import MyMap from './Map.tsx'
//import { BrowserRouter, Routes, Route, useLocation } from "react-router";
//import './App.css'
//import 'mapbox-gl/dist/mapbox-gl.css'
//import Dashboard from './Dashboard.tsx'

//function Activity() {
//    const { pathname } = useLocation();
//    const showMap = pathname === "/"; // or a Set/regex allowlist

//    // wrap so you don't need MyMap to forward `style`
//    return (
//        <div style={{ display: showMap ? "block" : "none" }}>
//            <MyMap />
//        </div>
//    );
//}

//export default function App() {
//    return (
//        <BrowserRouter>
//            <Activity />
//            <Routes>
//                <Route path="/" element={<div />} />
//                <Route path="/dashboard" element={<Dashboard />} />
//            </Routes>
//        </BrowserRouter>
//    );
//}
