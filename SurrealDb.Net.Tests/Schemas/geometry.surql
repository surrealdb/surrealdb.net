DEFINE TABLE geometry SCHEMAFULL;

DEFINE FIELD name ON geometry TYPE string;
DEFINE FIELD value ON geometry TYPE geometry<point | line | polygon | multipoint | multiline | multipolygon | collection>;

CREATE geometry:PointFromTuple SET name = "point-from-tuple", value = (-0.118092, 51.509865);

CREATE geometry:Point SET name = "point", value = {
    type: "Point",
    coordinates: [-0.118092, 51.509865],
};

CREATE geometry:LineString SET name = "line", value = {
	type: "LineString",
	coordinates: [
		[10.0, 11.2], [10.5, 11.9]
	]
};

CREATE geometry:Polygon SET name = "polygon", value = {
	type: "Polygon",
	coordinates: [[
		[-0.38314819, 51.37692386], [0.1785278, 51.37692386],
		[0.1785278, 51.61460570], [-0.38314819, 51.61460570],
		[-0.38314819, 51.37692386]
	]]
};

CREATE geometry:MultiPoint SET name = "multi-point", value = {
	type: "MultiPoint",
	coordinates: [
		[10.0, 11.2],
		[10.5, 11.9]
	],
};

CREATE geometry:MultiLineString SET name = "multi-line", value = {
	type: "MultiLineString",
	coordinates: [
		[ [10.0, 11.2], [10.5, 11.9] ],
		[ [11.0, 12.2], [11.5, 12.9], [12.0, 13.0] ]
	]
};

CREATE geometry:MultiPolygon SET name = "multi-polygon", value = {
	type: "MultiPolygon",
	coordinates: [
		[
			[ [10.0, 11.2], [10.5, 11.9], [10.8, 12.0], [10.0, 11.2] ]
		],
		[
			[ [9.0, 11.2], [10.5, 11.9], [10.3, 13.0], [9.0, 11.2] ]
		]
	]
};

CREATE geometry:GeometryCollection SET name = "collection", value = {
	type: "GeometryCollection",
	geometries: [
		{
			type: "MultiPoint",
			coordinates: [
				[10.0, 11.2],
				[10.5, 11.9]
			],
		},
		{
			type: "Polygon",
			coordinates: [[
				[-0.38314819, 51.37692386], [0.1785278, 51.37692386],
				[0.1785278, 51.61460570], [-0.38314819, 51.61460570],
				[-0.38314819, 51.37692386]
			]]
		},
		{
			type: "MultiPolygon",
			coordinates: [
				[
					[ [10.0, 11.2], [10.5, 11.9], [10.8, 12.0], [10.0, 11.2] ]
				],
				[
					[ [9.0, 11.2], [10.5, 11.9], [10.3, 13.0], [9.0, 11.2] ]
				]
			]
		}
	]
};