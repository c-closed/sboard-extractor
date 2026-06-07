export default {
	async fetch(request, env, ctx): Promise<Response> {
		const url = new URL(request.url);
		const corsHeaders = {
			'Access-Control-Allow-Origin': '*',
			'Access-Control-Allow-Methods': 'GET, OPTIONS',
			'Access-Control-Allow-Headers': 'Content-Type',
		};

		if (request.method === 'OPTIONS') {
			return new Response(null, { headers: corsHeaders, status: 204 });
		}

		if (url.pathname === '/api/update.xml' && request.method === 'GET') {
			const xml = `<?xml version="1.0" encoding="UTF-8"?>
<item>
    <version>1.0.0.0</version>
    <url>https://github.com/c-closed/sboard-extractor/releases/download/v1.0.0.0/SboardExtractor_Updated.zip</url>
    <mandatory>false</mandatory>
</item>`;
			return new Response(xml, {
				status: 200,
				headers: { 'Content-Type': 'text/xml; charset=utf-8', ...corsHeaders },
			});
		}

		return new Response('Not Found', { status: 404 });
	},
} satisfies ExportedHandler<Env>;
