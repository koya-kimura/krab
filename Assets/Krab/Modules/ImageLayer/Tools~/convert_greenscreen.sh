#!/bin/bash
# Converts green-screen clips into transparent PNG sequences for ImageLayer.
#
#   convert_greenscreen.sh <source dir> <output set dir>          write <set>/01/001.png …
#   convert_greenscreen.sh <source dir> <report dir> --preview    keyed frames over magenta, one sheet per clip
#
# Each clip is keyed with its own settings (CLIPS below), resampled to FPS and written as
# a zero-padded folder per clip. Requires ffmpeg with the chromakey and despill filters.
set -euo pipefail

FPS=12

# folder | source file | key color | similarity | blend | despill mix (0 = off) | extra filters after keying
CLIPS="
01|blue-shirt-green-gingham-5s.mp4|0x05F50C|0.16|0.06|0.5|
02|blue-shirt-green-pinstripe-side-5s.mp4|0x01EC0B|0.16|0.06|0.5|drawbox=x=0:y=0:w=450:h=12:color=black@0:t=fill:replace=1
03|blue-shirt-green-solid-5s.mp4|0x00F810|0.16|0.06|0.5|
04|bs-01-side-stripe-5s.mp4|0x00A405|0.14|0.06|0.5|
05|bs-02-below-lightblue-5s.mp4|0x05F60E|0.16|0.06|0.5|
06|bs-03-back-navy-5s.mp4|0x01EE0B|0.16|0.06|0.5|
07|bs-04-topdown-border-5s.mp4|0x06F411|0.16|0.06|0.5|
08|bs-05-lowleft-chambray-5s.mp4|0x02FD0B|0.16|0.06|0.5|
09|bs-06-dutch-turquoise-5s.mp4|0x01F20B|0.12|0.05|0|
"

if [ $# -lt 2 ]; then
    sed -n '2,8p' "$0"
    exit 1
fi

source_dir=$1
output_dir=$2
preview=${3:-}

mkdir -p "$output_dir"

echo "$CLIPS" | while IFS='|' read -r folder file color similarity blend despill extra; do
    [ -z "$folder" ] && continue

    filters="fps=$FPS,chromakey=$color:$similarity:$blend"
    [ "$despill" != "0" ] && filters="$filters,despill=type=green:mix=$despill"
    filters="$filters,format=rgba"
    [ -n "$extra" ] && filters="$filters,$extra"

    if [ "$preview" = "--preview" ]; then
        # Frames 0, 20, 40, 59 over magenta, side by side at half size.
        ffmpeg -nostdin -v error -y -i "$source_dir/$file" -f lavfi -i "color=c=magenta:s=1280x720" \
            -filter_complex "[0:v]$filters[k];[1:v][k]overlay=shortest=1,select='eq(n\,0)+eq(n\,20)+eq(n\,40)+eq(n\,59)',scale=640:-1,tile=4x1" \
            -frames:v 1 "$output_dir/GreenScreen-$folder.png"
    else
        mkdir -p "$output_dir/$folder"
        rm -f "$output_dir/$folder"/*.png
        ffmpeg -nostdin -v error -y -i "$source_dir/$file" -vf "$filters" -start_number 1 "$output_dir/$folder/%03d.png"
    fi

    echo "$folder  $file"
done
