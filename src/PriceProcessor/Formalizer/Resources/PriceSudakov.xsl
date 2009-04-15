<?xml version='1.0' encoding = "windows-1251" standalone = "yes"?>

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

	<xsl:output method="xml" indent="yes" encoding="windows-1251" />

	<xsl:template match="/" >
		
		<Price>

			<xsl:for-each select="Документ/Номенклатура/Элемент">
				<Position>
					<Name1>
						<xsl:value-of select="@Значение"/>
					</Name1>
					</Position>
			</xsl:for-each>

		</Price>
	</xsl:template>

</xsl:stylesheet>
